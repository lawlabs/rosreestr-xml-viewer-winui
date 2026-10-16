using System.Text.Json;
using RosreestrViewer.Core;

internal static class CorpusAudit
{
    public static async Task<int> RunAsync(string folder)
    {
        var files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Where(p => Path.GetExtension(p).ToLowerInvariant() is ".xml" or ".zip").ToArray();
        var rootCounts = new Dictionary<string, int>();
        var errors = new Dictionary<string, int>();
        var unique = new HashSet<string>();
        var importer = new ImportService();
        int processed = 0, records = 0, cad = 0, address = 0, area = 0, rights = 0, geometry = 0, attachments = 0, unknown = 0, pdf = 0, notices = 0;
        foreach (var file in files)
        {
            var result = await importer.ImportAsync([file]);
            processed++;
            foreach (var error in result.Errors)
            {
                // Keep only categories, never filenames, addresses or original values.
                string category = error.Contains("превышает") ? "size_limit" : error.Contains("XML/PDF не найден") ? "no_document" : error.Contains("путь") ? "attachment_path" : "parse_or_io";
                errors[category] = errors.GetValueOrDefault(category) + 1;
            }
            foreach (var d in result.Documents)
            {
                if (!unique.Add(d.Hash + "/" + d.CadastralNumber)) continue;
                records++; rootCounts[d.RootName] = rootCounts.GetValueOrDefault(d.RootName) + 1;
                if (d.CadastralNumber.Length > 0) cad++;
                if (d.Address.Length > 0) address++;
                if (d.Area.Length > 0) area++;
                if (d.Rights.Count > 0) rights++;
                if (d.Rings.Count > 0) geometry++;
                if (d.Attachments.Any(a => a.Content != null)) attachments++;
                if (!d.KnownFamily) unknown++;
                if (d.IsPdfDocument) pdf++;
                if (d.IsNotice) notices++;
            }
            if (processed % 2000 == 0) Console.WriteLine($"Processed {processed}/{files.Length} files; {records} distinct records.");
        }
        var report = new { files = files.Length, processed, distinctRecords = records, pdfDocuments = pdf, notices, withCadastralNumber = cad, withAddress = address, withArea = area, withRights = rights, withGeometry = geometry, withLocalAttachments = attachments, unknownFamilies = unknown, roots = rootCounts, errorCategories = errors };
        Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        var target = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "artifacts", "corpus-audit.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        await File.WriteAllTextAsync(target, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        return records > 0 ? 0 : 1;
    }
}
