using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using RosreestrViewer.Core;

namespace RosreestrViewer;

public sealed partial class MainPage
{
    int renderRevision;
    readonly SemaphoreSlim renderGate = new(1, 1);
    async Task InitializePreviewAsync()
    {
        try
        {
            Directory.CreateDirectory(previewFolder);
            var environment = await CoreWebView2Environment.CreateWithOptionsAsync(null, Path.Combine(dataFolder, "WebView2"), null);
            await EnsurePreviewAsync(environment);
            await Preview.EnsureCoreWebView2Async(environment);
            Preview.CoreWebView2.Settings.IsScriptEnabled = false;
            Preview.CoreWebView2.Settings.AreHostObjectsAllowed = false;
            Preview.CoreWebView2.Settings.AreDevToolsEnabled = false;
            Preview.CoreWebView2.Settings.IsWebMessageEnabled = false;
            Preview.CoreWebView2.SetVirtualHostNameToFolderMapping("viewer.local", previewFolder, CoreWebView2HostResourceAccessKind.DenyCors);
            Preview.CoreWebView2.NavigationStarting += (_, args) =>
            { if (!args.Uri.StartsWith("https://viewer.local/", StringComparison.OrdinalIgnoreCase) && args.Uri != "about:blank") args.Cancel = true; };
            Preview.CoreWebView2.NewWindowRequested += (_, args) => args.Handled = true;
            Preview.CoreWebView2.DownloadStarting += (_, args) => args.Cancel = true;
            try { printForms = await Task.Run(() => new PrintFormService(Path.Combine(AppContext.BaseDirectory, "Templates"))); }
            catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException or System.Text.Json.JsonException)
            { printFormsError = ex.Message; }
            ready = true; PreviewError.Visibility = Visibility.Collapsed;
            PdfButton.IsEnabled = selected != null && !busy;
            await RenderAsync();
        }
        catch (Exception ex)
        {
            Preview.Visibility = Visibility.Collapsed; PreviewError.Visibility = Visibility.Visible;
            PreviewErrorText.Text = "Проверьте наличие Microsoft Edge WebView2 Runtime. " + ex.Message;
            Notify("Не удалось открыть предпросмотр", ex.Message, InfoBarSeverity.Error);
        }
    }
    async void RetryPreview_Click(object sender, RoutedEventArgs e) => await InitializePreviewAsync();
    async Task RenderAsync()
    {
        if (!ready) return;
        int revision = ++renderRevision;
        await renderGate.WaitAsync();
        try
        {
            if (revision != renderRevision) return;
            AttachmentPane.Visibility = mode == "attachments" ? Visibility.Visible : Visibility.Collapsed;
            Preview.Visibility = mode == "attachments" ? Visibility.Collapsed : Visibility.Visible;
            var doc = selected; string viewMode = mode; string? compare = comparison; bool dark = ActualTheme == ElementTheme.Dark;
            FormInfo.Visibility = doc != null && viewMode is "form" or "originalPdf" && compare == null ? Visibility.Visible : Visibility.Collapsed;
            OriginalPdfPicker.Visibility = doc != null && viewMode == "originalPdf" && doc.DocumentPdfs.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
            if (doc != null && (viewMode == "originalPdf" || doc.IsPdfDocument && viewMode == "summary") && compare == null && SelectedOriginalPdf?.Content is { } originalPdf)
            {
                FormInfo.Title = "PDF из полученного пакета"; FormInfo.Message = SelectedOriginalPdf.Name + ". Исходный файл показывается без преобразования. Подлинность подписи не проверена."; FormInfo.Severity = InfoBarSeverity.Informational;
                var pdf = "original-" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(originalPdf)) + ".pdf";
                await File.WriteAllBytesAsync(Path.Combine(previewFolder, pdf), originalPdf);
                await NavigateAsync("https://viewer.local/" + pdf);
                return;
            }
            string html;
            if (doc != null && viewMode == "form" && compare == null)
            {
                var form = await GetPrintFormAsync(doc);
                if (revision != renderRevision) return;
                FormInfo.Title = form.Available ? form.Template!.Provenance + " · " + form.Template.Name : "Печатная форма недоступна";
                FormInfo.Message = form.Available ? "Таблицы и расположение — по XSL. " + (form.Warnings.Count == 0 ? "Совпадение переносов страниц с PDF Росреестра не гарантируется." : string.Join(" ", form.Warnings)) : form.Reason;
                FormInfo.Severity = !form.Available || form.Warnings.Count > 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Informational;
                html = form.Available ? form.Html : ReportRenderer.Page("<h1>Печатная форма недоступна</h1><p>" + ReportRenderer.E(form.Reason) + "</p><p>В меню «Шаблоны печатной формы» перечислены поддержанные пакеты. Для анализа выберите «Карточка»; содержание исходника доступно во вкладках XML.</p>", dark);
            }
            else html = await Task.Run(() =>
            {
                string body = compare ?? (doc == null ? ReportRenderer.Welcome() : viewMode switch
                {
                    "geometry" => GeometryRenderer.Html(doc), "fields" => ReportRenderer.Fields(doc),
                    "source" => doc.IsPdfDocument ? "<h1>Исходный PDF</h1><p>Документ открыт без преобразования. Сохранить оригинал можно во вкладке «Вложения».</p>" : "<h1>Исходный XML</h1><p class='muted'>Оригинальные байты доступны через «Сохранить исходный XML». Представление ниже форматируется для чтения.</p><pre>" + ReportRenderer.E(ReadSource(doc)) + "</pre>",
                    "originalPdf" => "<h1>Связанный PDF не найден</h1><p>Автоматически сопоставляются только PDF с именем исходного XML: document.pdf или document.xml.pdf. Другие PDF можно открыть во вкладке «Вложения».</p>",
                    _ => ReportRenderer.Card(doc, false)
                });
                return ReportRenderer.Page(body, dark);
            });
            string fileName = $"report-{revision}.html";
            await File.WriteAllTextAsync(Path.Combine(previewFolder, fileName), html, Encoding.UTF8);
            if (revision == renderRevision) await NavigateAsync("https://viewer.local/" + fileName);
        }
        catch (Exception ex) { if (revision == renderRevision) Notify("Ошибка предпросмотра", ex.Message, InfoBarSeverity.Error); }
        finally { renderGate.Release(); }
    }
    static string ReadSource(ExtractDocument doc)
    {
        using var m = new MemoryStream(doc.OriginalBytes);
        using var r = System.Xml.XmlReader.Create(m, ExtractParser.ReaderSettings());
        return System.Xml.Linq.XDocument.Load(r).ToString();
    }
    async Task NavigateAsync(string uri)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Completed(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        { if (sender.Source.Split('?')[0] == uri) { if (args.IsSuccess) tcs.TrySetResult(); else tcs.TrySetException(new IOException(args.WebErrorStatus.ToString())); } }
        Preview.CoreWebView2.NavigationCompleted += Completed;
        try { Preview.CoreWebView2.Navigate(uri); await tcs.Task.WaitAsync(TimeSpan.FromSeconds(15)); }
        finally { Preview.CoreWebView2.NavigationCompleted -= Completed; }
    }
    void RefreshAttachments()
    {
        AttachmentsList.Items.Clear();
        if (selected == null) return;
        foreach (var a in selected.Attachments) AttachmentsList.Items.Add(new ListViewItem { Content = a.Name + (a.Content != null ? "  ·  локальный файл" : a.IsRemote ? "  ·  ссылка в интернете" : a.IsServerReference ? "  ·  внутренний путь сервера" : "  ·  файл не найден"), Tag = a });
        if (selected.Attachments.Count == 0) AttachmentsList.Items.Add(new ListViewItem { Content = "Вложения не найдены", IsEnabled = false });
    }
    Attachment? ChosenAttachment => (AttachmentsList.SelectedItem as ListViewItem)?.Tag as Attachment;
    async void Download_Click(object sender, RoutedEventArgs e)
    {
        if (selected == null || busy) return;
        var doc = selected; var targets = doc.Attachments.Where(a => a.IsRemote && a.Content == null).ToArray();
        if (targets.Length == 0) { Notify("Загрузка графики", "В XML нет незагруженных ссылок на графические файлы."); return; }
        operation?.Dispose(); operation = new CancellationTokenSource(); SetBusy(true, "Загрузка графики с официальных сайтов…");
        int downloaded = 0; var errors = new List<string>();
        try
        {
            var service = new GraphicDownloadService(Path.Combine(dataFolder, "Graphics"));
            foreach (var a in targets)
            {
                try { doc.Attachments[doc.Attachments.IndexOf(a)] = await service.DownloadAsync(a, operation.Token); downloaded++; }
                catch (Exception ex) when (ex is not OperationCanceledException) { errors.Add(a.Name + ": " + ex.Message); }
            }
            RefreshAttachments(); Notify($"Загружено: {downloaded}", errors.Count == 0 ? "Графика доступна локально и сохранена в кэше." : string.Join("\n", errors), errors.Count == 0 ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
        }
        catch (OperationCanceledException) { Notify("Загрузка отменена", "Уже загруженные изображения остаются в кэше."); }
        finally { SetBusy(false, "Готово"); }
    }
    async void AttachmentPreview_Click(object sender, RoutedEventArgs e)
    {
        var a = ChosenAttachment; if (a?.Content == null || !ready) { Notify("Просмотр вложения", "Выберите доступный локальный файл."); return; }
        await renderGate.WaitAsync();
        try
        {
            if (!a.IsImage && !a.IsPdf) { Notify("Просмотр вложения", "Для подписи и TIFF используйте сохранение и соответствующее приложение."); return; }
            AttachmentPane.Visibility = Visibility.Collapsed; Preview.Visibility = Visibility.Visible;
            var name = "attachment-" + Guid.NewGuid().ToString("N") + a.Extension;
            await File.WriteAllBytesAsync(Path.Combine(previewFolder, name), a.Content);
            if (a.IsPdf) await NavigateAsync("https://viewer.local/" + name);
            else
            {
                var htmlName = name + ".html";
                await File.WriteAllTextAsync(Path.Combine(previewFolder, htmlName), ReportRenderer.Page($"<h1>{ReportRenderer.E(a.Name)}</h1><img class='plan' src='{name}' alt='Графическое вложение выписки'>", ActualTheme == ElementTheme.Dark));
                await NavigateAsync("https://viewer.local/" + htmlName);
            }
            Notify("Вложение", a.Name + ". Для возврата выберите другую вкладку.");
        }
        catch (Exception ex) { Notify("Просмотр вложения", ex.Message, InfoBarSeverity.Error); }
        finally { renderGate.Release(); }
    }
}
