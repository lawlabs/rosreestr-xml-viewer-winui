using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RosreestrViewer.Core;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.Windows.Storage.Pickers;

namespace RosreestrViewer;

public sealed partial class MainPage
{
    async Task<string?> SavePathAsync(string name, string extension)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        var picker = new FileSavePicker(App.MainWindow.AppWindow.Id) { SuggestedFileName = name, SuggestedStartLocation = PickerLocationId.DocumentsLibrary, ShowOverwritePrompt = true };
        picker.FileTypeChoices.Add(extension.TrimStart('.').ToUpperInvariant(), new List<string> { extension });
        var file = await picker.PickSaveFileAsync(); return file?.Path;
    }
    async void SaveAttachment_Click(object sender, RoutedEventArgs e)
    {
        var a = ChosenAttachment; if (a?.Content == null) { Notify("Сохранение", "Выберите локальное вложение."); return; }
        try { var path = await SavePathAsync(Path.GetFileNameWithoutExtension(a.Name), a.Extension); if (path != null) { await File.WriteAllBytesAsync(path, a.Content); Notify("Вложение сохранено", Path.GetFileName(path), InfoBarSeverity.Success); } }
        catch (Exception ex) { Notify("Сохранение вложения", ex.Message, InfoBarSeverity.Error); }
    }
    async void SaveOriginal_Click(object sender, RoutedEventArgs e)
    {
        if (selected == null) return;
        var doc = selected;
        try { var path = await SavePathAsync(Path.GetFileNameWithoutExtension(doc.EntryName), doc.IsPdfDocument ? ".pdf" : ".xml"); if (path != null) { await File.WriteAllBytesAsync(path, doc.OriginalBytes); Notify("Исходник сохранён", "Байты исходного документа сохранены без изменений.", InfoBarSeverity.Success); } }
        catch (Exception ex) { Notify("Сохранение XML", ex.Message, InfoBarSeverity.Error); }
    }
    async void Csv_Click(object sender, RoutedEventArgs e)
    {
        if (busy || documents.Count == 0) return;
        var snapshot = VisibleDocuments.Select(i => i.Document).ToArray();
        try { var path = await SavePathAsync("Реестр выписок", ".csv"); if (path != null) { await File.WriteAllTextAsync(path, RegistryService.Csv(snapshot), new UTF8Encoding(true)); Notify("Реестр сохранён", $"{snapshot.Length} выписок из текущей выборки", InfoBarSeverity.Success); } }
        catch (Exception ex) { Notify("Экспорт реестра", ex.Message, InfoBarSeverity.Error); }
    }
    async void Pdf_Click(object sender, RoutedEventArgs e)
    {
        if (selected == null || busy || !ready) return;
        var doc = selected; var snapshot = VisibleDocuments.Select(i => i.Document).ToArray();
        var format = new ComboBox { Header = "Представление документа", HorizontalAlignment = HorizontalAlignment.Stretch };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(format, "PdfFormat");
        if (doc.DocumentPdfs.Count > 0) format.Items.Add(new ComboBoxItem { Content = "PDF из пакета · исходные байты", Tag = "original" });
        format.Items.Add(new ComboBoxItem { Content = "Печатная форма XML · таблицы по XSL", Tag = "form" });
        format.Items.Add(new ComboBoxItem { Content = "Краткий отчёт приложения · карточка и схема", Tag = "report" });
        string preferred = mode == "summary" && !doc.IsPdfDocument ? "report" : doc.DocumentPdfs.Count > 0 && mode == "originalPdf" || doc.IsPdfDocument ? "original" : "form";
        format.SelectedItem = format.Items.OfType<ComboBoxItem>().First(i => (string)i.Tag == preferred);
        var content = new StackPanel { Spacing = 16 }; content.Children.Add(format);
        content.Children.Add(new TextBlock { TextWrapping = TextWrapping.Wrap, Text = "PDF из пакета сохраняется без преобразования. Печатная форма использует XSL с его таблицами и разделами. Для всей выборки формы и оригиналы сохраняются отдельными PDF в выбранную папку; краткий отчёт — одним файлом. Несколько строк одного исходного XML дают одну печатную форму." });
        var choice = await new ContentDialog { XamlRoot = XamlRoot, Title = "PDF и печать", Content = content, PrimaryButtonText = "Выбранная выписка", SecondaryButtonText = "Вся выборка", CloseButtonText = "Отмена" }.ShowAsync();
        if (choice == ContentDialogResult.None) return;
        string kind = (string)((ComboBoxItem)format.SelectedItem).Tag;
        var chosenPdf = SelectedOriginalPdf;
        try
        {
            if (choice == ContentDialogResult.Secondary && kind != "report")
            {
                var folder = await new FolderPicker(App.MainWindow.AppWindow.Id) { Title = "Папка для PDF текущей выборки" }.PickSingleFolderAsync();
                if (folder == null) return;
                var originals = snapshot.DistinctBy(d => d.Hash).ToArray();
                operation?.Dispose(); operation = new CancellationTokenSource(); SetBusy(true, "Пакетное сохранение PDF…");
                int saved = 0; var errors = new List<string>();
                await renderGate.WaitAsync();
                try
                {
                    foreach (var item in originals)
                    {
                        operation.Token.ThrowIfCancellationRequested();
                        try
                        {
                            if (kind == "original")
                            {
                                var pdfs = item.DocumentPdfs;
                                if (pdfs.Count != 1) { errors.Add(item.DisplayNumber + ": " + (pdfs.Count == 0 ? "нет связанного PDF" : "несколько связанных PDF; выберите нужный отдельно")); continue; }
                                await File.WriteAllBytesAsync(UniquePdfPath(folder.Path, item), pdfs[0].Content!, operation.Token);
                            }
                            else
                            {
                                var form = await GetPrintFormAsync(item);
                                if (!form.Available) { errors.Add(item.DisplayNumber + ": " + form.Reason); continue; }
                                await PrintHtmlToPdfAsync(form.Html, UniquePdfPath(folder.Path, item));
                            }
                            saved++; StatusText.Text = $"Сохранено: {saved} из {originals.Length}";
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException) { errors.Add(item.DisplayNumber + ": " + ex.Message); }
                    }
                }
                finally { renderGate.Release(); }
                Notify($"PDF сохранено: {saved}; пропущено: {errors.Count}", errors.Count == 0 ? "Каждый исходный документ сохранён отдельно. Существующие файлы не перезаписываются." : string.Join("\n", errors.Take(8)), errors.Count == 0 ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
                return;
            }
            if (kind == "original")
            {
                if (chosenPdf?.Content == null) { Notify("PDF", "Связанный PDF не выбран.", InfoBarSeverity.Warning); return; }
                var originalPath = await SavePathAsync(Path.GetFileNameWithoutExtension(chosenPdf.Name), ".pdf");
                if (originalPath != null) { await File.WriteAllBytesAsync(originalPath, chosenPdf.Content); Notify("PDF сохранён", "Исходный файл из пакета сохранён без преобразования.", InfoBarSeverity.Success); }
                return;
            }
            string html;
            if (kind == "form")
            {
                var form = await GetPrintFormAsync(doc);
                if (!form.Available) { Notify("Печатная форма", form.Reason, InfoBarSeverity.Warning); return; }
                html = form.Html;
            }
            else html = await Task.Run(() => ReportRenderer.Page(choice == ContentDialogResult.Primary ? ReportRenderer.Card(doc) : string.Join("", snapshot.Select(d => ReportRenderer.Card(d)))));
            var path = await SavePathAsync(choice == ContentDialogResult.Primary ? doc.DisplayNumber.Replace(':', '-') : "Выписки ЕГРН", ".pdf"); if (path == null) return;
            SetBusy(true, "Создание PDF…"); await renderGate.WaitAsync();
            try { await PrintHtmlToPdfAsync(html, path); }
            finally { renderGate.Release(); }
            Notify("PDF сохранён", (kind == "form" ? "Печатная форма по XSL. " : "Краткий отчёт приложения. ") + Path.GetFileName(path), InfoBarSeverity.Success);
        }
        catch (OperationCanceledException) { Notify("Сохранение отменено", "Уже созданные файлы остаются в выбранной папке."); }
        catch (Exception ex) { Notify("Экспорт PDF", ex.Message, InfoBarSeverity.Error); }
        finally { SetBusy(false, "Готово"); await RenderAsync(); }
    }
    async void Compare_Click(object sender, RoutedEventArgs e)
    {
        var pair = DocumentsList.SelectedItems.OfType<DocumentListItem>().Select(i => i.Document).ToArray();
        if (pair.Length != 2)
        {
            var items = VisibleDocuments.ToArray();
            if (items.Length < 2) { Notify("Сравнение", "Для сравнения откройте две выписки."); return; }
            var left = new ComboBox { Header = "Первая выписка", ItemsSource = items, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
            var right = new ComboBox { Header = "Вторая выписка", ItemsSource = items, SelectedIndex = 1, HorizontalAlignment = HorizontalAlignment.Stretch };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(left, "CompareFirst");
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(right, "CompareSecond");
            var panel = new StackPanel { Spacing = 16 }; panel.Children.Add(left); panel.Children.Add(right);
            if (await new ContentDialog { XamlRoot = XamlRoot, Title = "Сравнить выписки", Content = panel, PrimaryButtonText = "Сравнить", CloseButtonText = "Отмена" }.ShowAsync() != ContentDialogResult.Primary) return;
            pair = [((DocumentListItem)left.SelectedItem).Document, ((DocumentListItem)right.SelectedItem).Document];
        }
        ViewModes.SelectedItem = SummaryMode;
        comparison = await Task.Run(() => RegistryService.Compare(pair[0], pair[1])); mode = "summary"; await RenderAsync();
    }
    async void Svg_Click(object sender, RoutedEventArgs e)
    {
        if (selected == null || selected.Rings.Count == 0) { Notify("Экспорт SVG", "В выбранной выписке нет координат."); return; }
        try
        {
            var groups = GeometryRenderer.Systems(selected.Rings).ToArray();
            var rings = groups[0].ToArray();
            if (groups.Length > 1)
            {
                var combo = new ComboBox { Header = "Система координат", ItemsSource = groups.Select(g => g.Key.Length > 0 ? g.Key : "Не указана").ToArray(), SelectedIndex = 0 };
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(combo, "SchemeCoordinateSystem");
                if (await new ContentDialog { XamlRoot = XamlRoot, Title = "Экспорт схемы", Content = combo, PrimaryButtonText = "Продолжить", CloseButtonText = "Отмена" }.ShowAsync() != ContentDialogResult.Primary) return;
                rings = groups[combo.SelectedIndex].ToArray();
            }
            var path = await SavePathAsync(selected.DisplayNumber.Replace(':', '-') + " схема", ".svg");
            if (path != null) { await File.WriteAllTextAsync(path, GeometryRenderer.Svg(rings), Encoding.UTF8); Notify("SVG сохранён", "Схема по координатам XML; единицы исходной системы координат.", InfoBarSeverity.Success); }
        }
        catch (Exception ex) { Notify("Экспорт SVG", ex.Message, InfoBarSeverity.Error); }
    }
    async void ValidateSchema_Click(object sender, RoutedEventArgs e)
    {
        if (selected == null || selected.IsPdfDocument || busy) { Notify("Проверка XSD", "Выберите XML-выписку."); return; }
        var doc = selected;
        try
        {
            var picker = new FileOpenPicker(App.MainWindow.AppWindow.Id) { Title = "Выберите XSD для корня этого XML" }; picker.FileTypeFilter.Add(".xsd");
            var file = await picker.PickSingleFileAsync(); if (file == null) return;
            var bundlePicker = new FolderPicker(App.MainWindow.AppWindow.Id)
            { Title = "Корневая папка пакета XSD, включая SchemaCommon", SuggestedStartFolder = Path.GetDirectoryName(file.Path)! };
            var bundle = await bundlePicker.PickSingleFolderAsync(); if (bundle == null) return;
            SetBusy(true, "Проверка XML по выбранной XSD…");
            var result = await Task.Run(() => SchemaValidationService.Validate(doc.OriginalBytes, file.Path, bundle.Path));
            Notify(result.Valid ? "XSD: проверка пройдена" : "XSD: обнаружены ошибки", string.Join("\n", result.Messages.Take(8)), result.Valid ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
        }
        catch (Exception ex) { Notify("Проверка XSD", ex.Message, InfoBarSeverity.Error); }
        finally { SetBusy(false, "Готово"); }
    }
    async void Map_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            bool copied = selected?.CadastralNumber.Length > 0;
            if (copied) { var data = new DataPackage(); data.SetText(selected!.CadastralNumber); Clipboard.SetContent(data); }
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://nspd.gov.ru/map?thematic=PKK"));
            Notify("Публичная кадастровая карта", "НСПД открыта в браузере." + (copied ? " Кадастровый номер скопирован для поиска." : ""));
        }
        catch (Exception ex) { Notify("НСПД", ex.Message, InfoBarSeverity.Error); }
    }
    async void Signature_Click(object sender, RoutedEventArgs e)
    {
        try { await Windows.System.Launcher.LaunchUriAsync(new Uri("https://www.gosuslugi.ru/eds")); Notify("Проверка подписи", "Загрузите исходный XML и связанный SIG в официальном сервисе. Приложение файлы не отправляет."); }
        catch (Exception ex) { Notify("Проверка подписи", ex.Message, InfoBarSeverity.Error); }
    }
    async void Lists_Click(object sender, RoutedEventArgs e)
    {
        var first = new TextBox { Header = "Первый список или произвольный текст", AcceptsReturn = true, Height = 140, TextWrapping = TextWrapping.Wrap };
        var second = new TextBox { Header = "Второй список (необязательно)", AcceptsReturn = true, Height = 140, TextWrapping = TextWrapping.Wrap };
        var panel = new StackPanel { Spacing = 16 }; panel.Children.Add(first); panel.Children.Add(second);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(first, "FirstCadList"); Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(second, "SecondCadList");
        if (await new ContentDialog { XamlRoot = XamlRoot, Title = "Кадастровые номера", Content = panel, PrimaryButtonText = "Извлечь и сравнить", CloseButtonText = "Отмена" }.ShowAsync() != ContentDialogResult.Primary) return;
        ViewModes.SelectedItem = SummaryMode;
        comparison = RegistryService.CompareCadastralLists(first.Text, second.Text);
        mode = "summary"; await RenderAsync();
    }
    async void Theme_Click(object sender, RoutedEventArgs e)
    {
        var theme = ActualTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
        if (App.MainWindow.Content is FrameworkElement root) root.RequestedTheme = theme;
        RequestedTheme = ElementTheme.Default;
        await RenderAsync();
    }
    async void About_Click(object sender, RoutedEventArgs e) => await new ContentDialog
    {
        XamlRoot = XamlRoot, Title = "Выписки ЕГРН · WinUI 3", CloseButtonText = "Закрыть",
        Content = "Локальное рабочее пространство для XML и ZIP: карточки объектов, права, полный список полей, вложения, схемы по координатам, CSV и PDF.\n\nРаспознаются известные узлы семейств extract_about_property, extract_base_params, перехода прав, КПТ и старых выписок. Полное соответствие каждой версии XSD пока не сертифицировано.\n\nНаличие SIG не означает успешную проверку подписи. Персональные данные могут отсутствовать в полученной выписке. Граница строится только из данных XML; заказ выписок выполняется через официальные сервисы.\n\nИсследование и ограничения: docs/RESEARCH.md в проекте."
    }.ShowAsync();
}
