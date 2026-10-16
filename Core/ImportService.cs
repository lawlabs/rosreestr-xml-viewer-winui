using System.IO.Compression;

namespace RosreestrViewer.Core;

public sealed class ImportService
{
    public const int MaxAttachmentBytes = 24 * 1024 * 1024;
    public const long MaxPackageBytes = 256L * 1024 * 1024;
    public async Task<ImportResult> ImportAsync(IEnumerable<string> paths, CancellationToken token = default) =>
        await Task.Run(() => Import(paths, token), token);
    ImportResult Import(IEnumerable<string> paths, CancellationToken token)
    {
        var docs = new List<ExtractDocument>();
        var errors = new List<string>();
        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            token.ThrowIfCancellationRequested();
            try
            {
                if (Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase)) ReadZip(path, docs, errors, token);
                else if (Path.GetExtension(path).Equals(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    var parsed = ExtractParser.Parse(ReadFile(path, ExtractParser.MaxXmlBytes), path, Path.GetFileName(path));
                    foreach (var doc in parsed)
                    {
                        var folder = Path.GetDirectoryName(Path.GetFullPath(path))!;
                        foreach (var reference in doc.Attachments.ToArray().Where(a => !a.IsRemote && !a.IsServerReference))
                        {
                            try
                            {
                                string local = SafeLocalPath(folder, reference.Reference);
                                if (File.Exists(local)) ReplaceAttachment(doc, reference, ReadFile(local, MaxAttachmentBytes));
                                else doc.Warnings.Add($"Вложение не найдено: {reference.Name}");
                            }
                            catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException) { doc.Warnings.Add($"Вложение {reference.Name}: {ex.Message}"); }
                        }
                        foreach (var suffix in new[] { ".sig", ".p7s", ".pdf" })
                            foreach (var sibling in new[] { path + suffix, Path.ChangeExtension(path, suffix) }.Distinct())
                                if (File.Exists(sibling)) AddSibling(doc, Path.GetFileName(sibling), sibling, () => ReadFile(sibling, MaxAttachmentBytes), suffix == ".pdf");
                        docs.Add(doc);
                    }
                }
                else if (Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    var doc = Pdf(ReadFile(path, MaxAttachmentBytes), path, Path.GetFileName(path));
                    foreach (var signature in new[] { path + ".sig", Path.ChangeExtension(path, ".sig") }.Distinct())
                        if (File.Exists(signature)) doc.Attachments.Add(new Attachment(Path.GetFileName(signature), signature, ReadFile(signature, MaxAttachmentBytes)));
                    docs.Add(doc);
                }
                else errors.Add($"{Path.GetFileName(path)}: для импорта выберите XML или ZIP; PDF и SIG читаются как вложения.");
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or System.Xml.XmlException or UnauthorizedAccessException or ArgumentException)
            { errors.Add($"{Path.GetFileName(path)}: {ex.Message}"); }
        }
        return new ImportResult(docs, errors);
    }
    static void ReadZip(string path, List<ExtractDocument> docs, List<string> errors, CancellationToken token)
    {
        using var zip = ZipFile.OpenRead(path);
        if (zip.Entries.Count > 4096 || zip.Entries.Any(e => e.Length > MaxPackageBytes) || zip.Entries.Sum(e => e.Length) > MaxPackageBytes) throw new InvalidDataException("Архив превышает лимит: 4096 файлов или 256 МБ после распаковки.");
        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in zip.Entries)
        {
            var normalized = NormalizeEntry(e.FullName);
            if (e.Name.Length > 0 && !entries.TryAdd(normalized, e)) throw new InvalidDataException("Архив содержит неоднозначные имена файлов.");
        }
        var xmlEntries = entries.Values.Where(e => Path.GetExtension(e.Name).Equals(".xml", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (xmlEntries.Length == 0)
        {
            var pdfEntries = entries.Values.Where(e => Path.GetExtension(e.Name).Equals(".pdf", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (pdfEntries.Length == 0) errors.Add($"{Path.GetFileName(path)}: XML/PDF не найден. Вложенные ZIP автоматически не распаковываются.");
            foreach (var e in pdfEntries)
            {
                token.ThrowIfCancellationRequested();
                try { docs.Add(Pdf(ReadEntry(e, MaxAttachmentBytes), path, e.FullName)); }
                catch (Exception ex) when (ex is IOException or InvalidDataException) { errors.Add($"{e.Name}: {ex.Message}"); }
            }
        }
        foreach (var e in xmlEntries)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                var parsed = ExtractParser.Parse(ReadEntry(e, ExtractParser.MaxXmlBytes), path, e.FullName);
                foreach (var doc in parsed)
                {
                    string dir = Path.GetDirectoryName(e.FullName.Replace('/', Path.DirectorySeparatorChar))?.Replace('\\', '/') ?? "";
                    foreach (var reference in doc.Attachments.ToArray().Where(a => !a.IsRemote && !a.IsServerReference))
                    {
                        try
                        {
                            string refPath = NormalizeEntry(dir.Length == 0 ? reference.Reference : dir + "/" + reference.Reference);
                            if (entries.TryGetValue(refPath, out var asset)) ReplaceAttachment(doc, reference, ReadEntry(asset, MaxAttachmentBytes));
                            else doc.Warnings.Add($"Вложение не найдено в архиве: {reference.Name}");
                        }
                        catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException)
                        { doc.Warnings.Add($"Вложение {reference.Name}: {ex.Message}"); }
                    }
                    foreach (var suffix in new[] { ".sig", ".p7s", ".pdf" })
                        foreach (var name in new[] { e.FullName + suffix, Path.ChangeExtension(e.FullName, suffix) }.Distinct())
                            if (entries.TryGetValue(NormalizeEntry(name), out var sibling))
                                AddSibling(doc, sibling.Name, sibling.FullName, () => ReadEntry(sibling, MaxAttachmentBytes), suffix == ".pdf");
                    docs.Add(doc);
                }
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or System.Xml.XmlException or ArgumentException)
            { errors.Add($"{Path.GetFileName(path)} / {e.Name}: {ex.Message}"); }
        }
        // Keep unpaired PDFs visible even when the archive also contains XML.
        // Different filenames are not evidence that a PDF belongs to a particular XML.
        if (xmlEntries.Length > 0)
        {
            var paired = docs.Where(d => d.SourcePath == path).SelectMany(d => d.Attachments)
                .Where(a => a.IsPdf && a.Content != null).Select(a => a.Reference).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var e in entries.Values.Where(e => Path.GetExtension(e.Name).Equals(".pdf", StringComparison.OrdinalIgnoreCase) && !paired.Contains(e.FullName)))
            {
                token.ThrowIfCancellationRequested();
                try { docs.Add(Pdf(ReadEntry(e, MaxAttachmentBytes), path, e.FullName)); }
                catch (Exception ex) when (ex is IOException or InvalidDataException) { errors.Add($"{e.Name}: {ex.Message}"); }
            }
        }
    }
    static void ReplaceAttachment(ExtractDocument doc, Attachment old, byte[] data) { doc.Attachments[doc.Attachments.IndexOf(old)] = old with { Content = data }; }
    static void AddSibling(ExtractDocument doc, string name, string reference, Func<byte[]> read, bool documentPdf)
    {
        try
        {
            var data = read();
            if (documentPdf && !data.AsSpan().StartsWith("%PDF-"u8)) { doc.Warnings.Add("Связанный PDF имеет неверный заголовок: " + name); return; }
            var existing = doc.Attachments.FindIndex(a => a.Reference.Equals(reference, StringComparison.OrdinalIgnoreCase)
                || a.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && a.Content != null && a.Content.AsSpan().SequenceEqual(data));
            if (existing >= 0) doc.Attachments[existing] = doc.Attachments[existing] with { Content = data, IsDocumentPdf = documentPdf };
            else doc.Attachments.Add(new Attachment(name, reference, data, documentPdf));
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException)
        { doc.Warnings.Add("Связанный файл " + name + ": " + ex.Message); }
    }
    static ExtractDocument Pdf(byte[] bytes, string source, string entry)
    {
        if (!bytes.AsSpan().StartsWith("%PDF-"u8)) throw new InvalidDataException("Файл не является PDF.");
        var d = new ExtractDocument { SourcePath = source, EntryName = entry, OriginalBytes = bytes, Hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)), RootName = "PDF", IsPdfDocument = true, KnownFamily = true, Kind = "Документ PDF" };
        d.Attachments.Add(new Attachment(Path.GetFileName(entry), entry, bytes, IsDocumentPdf: true));
        d.Warnings.Add("Документ PDF открыт без извлечения структурированных сведений. Для реестра объектов и координат откройте связанный XML.");
        return d;
    }
    public static string NormalizeEntry(string entry)
    {
        var s = entry.Replace('\\', '/');
        if (s.StartsWith('/') || s.Contains(':') || s.Split('/').Any(p => p is ".." or ".")) throw new InvalidDataException("Недопустимый путь вложения.");
        return s;
    }
    public static string SafeLocalPath(string folder, string reference)
    {
        var s = NormalizeEntry(reference);
        var prefix = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var result = Path.GetFullPath(Path.Combine(folder, s));
        if (!result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Вложение выходит за папку документа.");
        return result;
    }
    static byte[] ReadFile(string path, int max) { if (new FileInfo(path).Length > max) throw new InvalidDataException("Размер файла превышает допустимый лимит."); using var f = File.OpenRead(path); return ReadBounded(f, max); }
    static byte[] ReadEntry(ZipArchiveEntry e, int max) { if (e.Length > max) throw new InvalidDataException("Размер вложения превышает допустимый лимит."); using var s = e.Open(); return ReadBounded(s, max); }
    public static byte[] ReadBounded(Stream s, int max)
    {
        using var m = new MemoryStream();
        var buffer = new byte[81920]; int n;
        while ((n = s.Read(buffer)) > 0) { if (m.Length + n > max) throw new InvalidDataException("Размер файла превышает допустимый лимит."); m.Write(buffer, 0, n); }
        return m.ToArray();
    }
}
