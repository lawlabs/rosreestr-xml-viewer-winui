using System.Text.Json;
using RosreestrViewer.Core;

internal static class PrintFormAudit
{
    public static async Task<int> Run(string folder, string templates)
    {
        var service = new PrintFormService(templates);
        var counts = new Dictionary<string, int>();
        var samples = new Dictionary<string, int>();
        var failures = new Dictionary<string, int>();
        var seen = new HashSet<string>(); int scanned = 0, transformed = 0, withPdf = 0;
        foreach (var path in Directory.EnumerateFiles(folder, "*.xml", SearchOption.AllDirectories))
        {
            var batch = await new ImportService().ImportAsync([path]); scanned++;
            var doc = batch.Documents.FirstOrDefault();
            if (doc != null && seen.Add(doc.Hash))
            {
                if (doc.DocumentPdfs.Count > 0) withPdf++;
                if (samples.GetValueOrDefault(doc.RootName) < 12)
                {
                    samples[doc.RootName] = samples.GetValueOrDefault(doc.RootName) + 1;
                    var form = service.Render(doc);
                    if (form.Available) { transformed++; counts[form.Template!.Id] = counts.GetValueOrDefault(form.Template.Id) + 1; }
                    else
                    {
                        string kind = form.Reason.StartsWith("Печатная форма не сформирована:") ? "transform_error" : "no_matching_form";
                        failures[doc.RootName + "/" + kind] = failures.GetValueOrDefault(doc.RootName + "/" + kind) + 1;
                    }
                }
            }
            if (scanned % 1000 == 0) Console.WriteLine($"Scanned {scanned} source XML files; {transformed} representative forms rendered.");
        }
        var report = new { sourceXmlFiles = scanned, uniqueSourceXml = seen.Count, withPairedPdf = withPdf,
            sampling = "first 12 distinct source XML per root; not an exhaustive layout comparison", sampled = samples,
            rendered = counts, failureCategories = failures };
        string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(json);
        string target = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "artifacts", "print-form-audit.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(target)!); await File.WriteAllTextAsync(target, json);
        return transformed > 0 ? 0 : 1;
    }
}
