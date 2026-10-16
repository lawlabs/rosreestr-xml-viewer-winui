using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RosreestrViewer.Core;

namespace RosreestrViewer;

public sealed partial class MainPage
{
    PrintFormService? printForms;
    string printFormsError = "Пакет XSL ещё не загружен.";
    bool updatingOriginalPdfs;
    Attachment? SelectedOriginalPdf => (OriginalPdfPicker.SelectedItem as ComboBoxItem)?.Tag as Attachment;
    Task<PrintFormResult> GetPrintFormAsync(ExtractDocument document) => Task.Run(() => printForms?.Render(document)
        ?? new PrintFormResult(false, "", null, "Не удалось загрузить локальные XSL: " + printFormsError, []));
    void RefreshOriginalPdfs()
    {
        updatingOriginalPdfs = true;
        try
        {
            OriginalPdfPicker.Items.Clear();
            if (selected != null) foreach (var pdf in selected.DocumentPdfs) OriginalPdfPicker.Items.Add(new ComboBoxItem { Content = pdf.Name, Tag = pdf });
            OriginalPdfPicker.SelectedIndex = OriginalPdfPicker.Items.Count > 0 ? 0 : -1;
            OriginalPdfMode.IsEnabled = OriginalPdfPicker.Items.Count > 0;
        }
        finally { updatingOriginalPdfs = false; }
    }
    async void OriginalPdf_Changed(object sender, SelectionChangedEventArgs e)
    { if (!updatingOriginalPdfs && mode == "originalPdf") await RenderAsync(); }
    async void SaveFormHtml_Click(object sender, RoutedEventArgs e)
    {
        if (selected == null || busy) return;
        var document = selected;
        try
        {
            var form = await GetPrintFormAsync(document);
            if (!form.Available) { Notify("Печатная форма", form.Reason, InfoBarSeverity.Warning); return; }
            var path = await SavePathAsync(document.DisplayNumber.Replace(':', '-') + " печатная форма", ".html");
            if (path == null) return;
            await File.WriteAllTextAsync(path, form.Html, new UTF8Encoding(false));
            Notify("HTML сохранён", form.Template!.Provenance + ". Все доступные изображения включены в файл; исходный XML сохранён без изменений.", InfoBarSeverity.Success);
        }
        catch (Exception ex) { Notify("Сохранение HTML", ex.Message, InfoBarSeverity.Error); }
    }
    async void Templates_Click(object sender, RoutedEventArgs e)
    {
        string details = printForms == null ? printFormsError : string.Join("\n\n", printForms.Templates.Select(t => t.Name + "\n" + t.Provenance + "\n" + t.Source));
        await new ContentDialog
        {
            XamlRoot = XamlRoot, Title = "Шаблоны печатной формы", CloseButtonText = "Закрыть",
            Content = new ScrollViewer { MaxHeight = 480, Content = new TextBlock { TextWrapping = TextWrapping.Wrap,
                Text = "Шаблон выбирается по корню, namespace, версии и ссылке xml-stylesheet. Пакеты хранятся локально с SHA-256; внешний XSL автоматически не выполняется.\n\n" + details } }
        }.ShowAsync();
    }
    async Task PrintHtmlToPdfAsync(string html, string destination)
    {
        string token = Guid.NewGuid().ToString("N");
        string htmlPath = Path.Combine(previewFolder, "print-" + token + ".html");
        string pdfPath = Path.Combine(previewFolder, "print-" + token + ".pdf");
        try
        {
            await File.WriteAllTextAsync(htmlPath, html);
            await NavigateAsync("https://viewer.local/" + Path.GetFileName(htmlPath));
            await Preview.EnsureCoreWebView2Async();
            var settings = Preview.CoreWebView2.Environment.CreatePrintSettings();
            settings.PageWidth = 210 / 25.4; settings.PageHeight = 297 / 25.4;
            settings.MarginTop = settings.MarginBottom = settings.MarginLeft = settings.MarginRight = 12 / 25.4;
            settings.ShouldPrintBackgrounds = true;
            if (!await Preview.CoreWebView2.PrintToPdfAsync(pdfPath, settings)) throw new IOException("WebView2 не смог сохранить PDF.");
            File.Copy(pdfPath, destination, overwrite: true);
        }
        finally
        {
            if (File.Exists(htmlPath)) File.Delete(htmlPath);
            if (File.Exists(pdfPath)) File.Delete(pdfPath);
        }
    }
    static string UniquePdfPath(string folder, ExtractDocument doc)
    {
        string name = (doc.IsPdfDocument ? Path.GetFileNameWithoutExtension(doc.EntryName) : doc.DisplayNumber).Replace(':', '-');
        foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        if (name.Length > 100) name = name[..100];
        string basis = Path.Combine(folder, name + "-" + doc.Hash[..8]);
        string path = basis + ".pdf"; int suffix = 2;
        while (File.Exists(path)) path = basis + " (" + suffix++ + ").pdf";
        return path;
    }
}
