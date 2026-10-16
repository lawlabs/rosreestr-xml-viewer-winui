using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RosreestrViewer.Core;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.Windows.Storage.Pickers;
using Microsoft.Web.WebView2.Core;

namespace RosreestrViewer;

/// <summary>Coordinates WinUI interactions; document processing lives in Core.</summary>
public sealed partial class MainPage : Page
{
    public ObservableCollection<DocumentListItem> VisibleDocuments { get; } = [];
    readonly List<ExtractDocument> documents = [];
    readonly HashSet<string> documentKeys = new(StringComparer.Ordinal);
    readonly HashSet<string> sessionPaths = new(StringComparer.OrdinalIgnoreCase);
    readonly ImportService importer = new();
    readonly string dataFolder = Path.Combine(Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path, "Local", App.IsTestProfile ? "RosreestrViewer-Test" : "RosreestrViewer");
    readonly string previewFolder;
    ExtractDocument? selected;
    string mode = "form";
    string? comparison;
    bool ready, busy, pageLoaded;
    CancellationTokenSource? operation;
    public MainPage()
    {
        InitializeComponent();
        previewFolder = Path.Combine(dataFolder, "Preview", Guid.NewGuid().ToString("N"));
        ViewModes.SelectedItem = PrintFormMode;
        ActualThemeChanged += (_, _) => _ = RenderAsync();
    }
    async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (pageLoaded) return;
        pageLoaded = true;
        await InitializePreviewAsync();
        App.MainWindow.Closed += (_, _) =>
        {
            operation?.Cancel(); Preview.Close();
            try { File.WriteAllLines(Path.Combine(dataFolder, "session.txt"), sessionPaths); }
            catch (IOException) { }
            // The directory is created from the fixed application folder and a generated GUID.
            try { if (Directory.Exists(previewFolder)) Directory.Delete(previewFolder, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        };
    }
    void Notify(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational)
    { Notice.Title = title; Notice.Message = message; Notice.Severity = severity; Notice.IsOpen = true; }
    void SetBusy(bool value, string text)
    {
        busy = value; StatusText.Text = text; BusyRing.IsActive = value; BusyRing.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        PdfButton.IsEnabled = !value && selected != null && ready; CsvButton.IsEnabled = !value && documents.Count > 0;
    }
    async Task EnsurePreviewAsync(CoreWebView2Environment environment) => await Preview.EnsureCoreWebView2Async(environment);
    async void Open_Click(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        try
        {
            var picker = new FileOpenPicker(App.MainWindow.AppWindow.Id) { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
            picker.FileTypeFilter.Add(".xml"); picker.FileTypeFilter.Add(".zip"); picker.FileTypeFilter.Add(".pdf");
            var files = await picker.PickMultipleFilesAsync(); await ImportAsync(files.Select(f => f.Path));
        }
        catch (Exception ex) { Notify("Открытие файлов", ex.Message, InfoBarSeverity.Error); }
    }
    async void Folder_Click(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        try
        {
            var picker = new FolderPicker(App.MainWindow.AppWindow.Id);
            var folder = await picker.PickSingleFolderAsync();
            if (folder == null) return;
            var paths = await Task.Run(() => Directory.EnumerateFiles(folder.Path).Where(f => Path.GetExtension(f).ToLowerInvariant() is ".xml" or ".zip" or ".pdf").Take(1001).ToArray());
            if (paths.Length > 1000) { Notify("Слишком много файлов", "Выберите папку с 1000 или менее XML/ZIP.", InfoBarSeverity.Warning); return; }
            await ImportAsync(paths);
        }
        catch (Exception ex) { Notify("Открытие папки", ex.Message, InfoBarSeverity.Error); }
    }
    async Task ImportAsync(IEnumerable<string> paths)
    {
        var items = paths.ToArray(); if (items.Length == 0 || busy) return;
        if (items.Length > 1000) { Notify("Лимит импорта", "За один раз можно открыть до 1000 файлов.", InfoBarSeverity.Warning); return; }
        operation?.Dispose(); operation = new CancellationTokenSource(); SetBusy(true, "Чтение документов…");
        try
        {
            var result = await importer.ImportAsync(items, operation.Token);
            foreach (var source in result.Documents.Select(d => d.SourcePath)) sessionPaths.Add(source);
            int added = 0; ExtractDocument? lastImported = null;
            foreach (var doc in result.Documents)
            {
                if (documentKeys.Add(doc.Hash + "/" + doc.CadastralNumber)) { documents.Add(doc); added++; lastImported = doc; }
                else
                {
                    var existing = documents.First(d => d.Hash == doc.Hash && d.CadastralNumber == doc.CadastralNumber);
                    foreach (var asset in doc.Attachments)
                    {
                        int index = existing.Attachments.FindIndex(a => a.Reference.Equals(asset.Reference, StringComparison.OrdinalIgnoreCase)
                            || a.Name.Equals(asset.Name, StringComparison.OrdinalIgnoreCase) && a.Content != null && asset.Content != null && a.Content.AsSpan().SequenceEqual(asset.Content));
                        if (index < 0) existing.Attachments.Add(asset);
                        else if (asset.Content != null) existing.Attachments[index] = asset;
                    }
                    lastImported = existing;
                }
            }
            comparison = null; ApplyFilter();
            if (VisibleDocuments.Count > 0) DocumentsList.SelectedItem = VisibleDocuments.FirstOrDefault(i => i.Document == lastImported) ?? VisibleDocuments[^1];
            Directory.CreateDirectory(dataFolder);
            await File.WriteAllLinesAsync(Path.Combine(dataFolder, "session.txt"), sessionPaths);
            Notify($"Добавлено: {added}", result.Errors.Count == 0 ? "Исходные документы прочитаны без изменений. Выберите выписку в списке." : string.Join("\n", result.Errors.Take(12)), result.Errors.Count == 0 ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
            StatusText.Text = $"Документов: {documents.Count} · ошибок импорта: {result.Errors.Count}";
        }
        catch (OperationCanceledException) { StatusText.Text = "Импорт отменён"; }
        catch (Exception ex) { Notify("Ошибка импорта", ex.Message, InfoBarSeverity.Error); }
        finally { SetBusy(false, StatusText.Text); }
    }
    void Search_TextChanged(object sender, TextChangedEventArgs e) { if (DocumentsList != null) ApplyFilter(); }
    void ApplyFilter()
    {
        string query = SearchBox.Text.Trim(); var previous = selected;
        VisibleDocuments.Clear();
        foreach (var d in documents.Where(d => query.Length == 0 || (d.CadastralNumber + " " + d.Address + " " + d.Kind + " " + d.EntryName + " " + string.Join(" ", d.Rights.Select(r => r.Holder))).Contains(query, StringComparison.OrdinalIgnoreCase))) VisibleDocuments.Add(new DocumentListItem(d));
        if (previous != null) DocumentsList.SelectedItem = VisibleDocuments.FirstOrDefault(i => i.Document == previous);
        LibraryCount.Text = $"{VisibleDocuments.Count} из {documents.Count} выписок";
        CsvButton.IsEnabled = documents.Count > 0 && !busy;
    }
    async void Documents_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        selected = (e.AddedItems.OfType<DocumentListItem>().LastOrDefault() ?? DocumentsList.SelectedItems.OfType<DocumentListItem>().LastOrDefault())?.Document;
        comparison = null;
        DocumentTitle.Text = selected?.DisplayNumber ?? "Выписки ЕГРН";
        DocumentSubtitle.Text = selected == null ? "XML · архивы · графика · реестр" : selected.Kind + " · " + selected.GeometryStatus;
        PdfButton.IsEnabled = selected != null && ready && !busy;
        RefreshAttachments(); RefreshOriginalPdfs();
        if (selected != null && mode == "form" && selected.DocumentPdfs.Count > 0) ViewModes.SelectedItem = OriginalPdfMode;
        else if (mode == "originalPdf" && selected?.DocumentPdfs.Count is not > 0) ViewModes.SelectedItem = PrintFormMode;
        await RenderAsync();
    }
    async void ViewMode_Changed(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    { mode = (args.SelectedItem as NavigationViewItem)?.Tag?.ToString() ?? "summary"; comparison = null; await RenderAsync(); }
    async void Demo_Click(object sender, RoutedEventArgs e)
    {
        try { await ImportAsync(Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "Samples"), "*.xml")); }
        catch (Exception ex) { Notify("Демонстрационный набор", ex.Message, InfoBarSeverity.Error); }
    }
    async void Restore_Click(object sender, RoutedEventArgs e)
    {
        try { var file = Path.Combine(dataFolder, "session.txt"); if (File.Exists(file)) await ImportAsync(await File.ReadAllLinesAsync(file)); else Notify("Последняя сессия", "Сохранённой сессии пока нет."); }
        catch (Exception ex) { Notify("Восстановление сессии", ex.Message, InfoBarSeverity.Error); }
    }
    async void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        if (await new ContentDialog { XamlRoot = XamlRoot, Title = "Очистить рабочий список?", Content = $"Из списка будет убрано {documents.Count} выписок. Исходные файлы останутся на диске.", PrimaryButtonText = "Очистить", CloseButtonText = "Отмена" }.ShowAsync() != ContentDialogResult.Primary) return;
        documents.Clear(); documentKeys.Clear(); sessionPaths.Clear(); selected = null; comparison = null; ApplyFilter();
        try { await File.WriteAllTextAsync(Path.Combine(dataFolder, "session.txt"), ""); }
        catch (IOException ex) { Notify("Сохранение сессии", ex.Message, InfoBarSeverity.Warning); }
        await RenderAsync();
    }
    void Cancel_Click(object sender, RoutedEventArgs e) => operation?.Cancel();
    void Page_DragOver(object sender, DragEventArgs e) { e.AcceptedOperation = DataPackageOperation.Copy; }
    async void Page_Drop(object sender, DragEventArgs e)
    {
        try { if (e.DataView.Contains(StandardDataFormats.StorageItems)) { var items = await e.DataView.GetStorageItemsAsync(); await ImportAsync(items.Select(i => i.Path)); } }
        catch (Exception ex) { Notify("Перетаскивание файлов", ex.Message, InfoBarSeverity.Error); }
    }
    void Root_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (LibraryColumn == null) return;
        bool narrow = e.NewSize.Width < 850;
        LibraryColumn.Width = new GridLength(narrow ? 235 : 308);
        LocalLabel.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible;
        if (e.NewSize.Width < 620)
        {
            WorkspaceGrid.RowDefinitions.Clear(); WorkspaceGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(220) }); WorkspaceGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            LibraryColumn.Width = new GridLength(0); Grid.SetColumn(LibraryPane, 1); Grid.SetRow(LibraryPane, 0); Grid.SetRow(DetailPane, 1);
        }
        else { WorkspaceGrid.RowDefinitions.Clear(); Grid.SetColumn(LibraryPane, 0); Grid.SetRow(LibraryPane, 0); Grid.SetRow(DetailPane, 0); }
    }
}
