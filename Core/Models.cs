namespace RosreestrViewer.Core;

public sealed record XmlField(string Path, string Name, string Value);
public sealed record BoundaryPoint(double X, double Y, string Number, string Accuracy);
public sealed record BoundaryRing(string Name, string CoordinateSystem, IReadOnlyList<BoundaryPoint> Points);
public sealed record RightEntry(string Holder, string Type, string Number, string Date, string Share);
public sealed record Attachment(string Name, string Reference, byte[]? Content = null, bool IsDocumentPdf = false)
{
    public bool IsRemote => Uri.TryCreate(Reference, UriKind.Absolute, out var u) && u.Scheme is "https" or "http";
    public bool IsServerReference => Reference.StartsWith('/') || Reference.StartsWith("\\\\", StringComparison.Ordinal) || Reference.EndsWith("/EXT_FILE", StringComparison.OrdinalIgnoreCase);
    public string Extension => Path.GetExtension(Content != null ? Name : IsRemote ? new Uri(Reference).AbsolutePath : Name).ToLowerInvariant();
    public bool IsImage => Extension is ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp";
    public bool IsPdf => Extension == ".pdf";
}
public sealed class ExtractDocument
{
    public required string SourcePath { get; init; }
    public required string EntryName { get; init; }
    public required byte[] OriginalBytes { get; init; }
    public required string Hash { get; init; }
    public required string RootName { get; init; }
    public string Namespace { get; init; } = "";
    public string SchemaVersion { get; init; } = "";
    public string CadastralNumber { get; init; } = "";
    public string Kind { get; init; } = "";
    public string Address { get; init; } = "";
    public string Area { get; init; } = "";
    public string Date { get; init; } = "";
    public string Purpose { get; init; } = "";
    public string Cost { get; init; } = "";
    public string Status { get; init; } = "";
    public bool KnownFamily { get; init; }
    public bool IsNotice { get; init; }
    public bool IsPdfDocument { get; init; }
    public string NoticeReason { get; init; } = "";
    public IReadOnlyList<XmlField> Fields { get; init; } = [];
    public IReadOnlyList<RightEntry> Rights { get; init; } = [];
    public IReadOnlyList<string> Encumbrances { get; init; } = [];
    public IReadOnlyList<BoundaryRing> Rings { get; init; } = [];
    public List<Attachment> Attachments { get; } = [];
    public List<string> Warnings { get; } = [];
    public IReadOnlyList<Attachment> DocumentPdfs => Attachments.Where(a => a.IsDocumentPdf && a.IsPdf && a.Content != null)
        .DistinctBy(a => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(a.Content!))).ToArray();
    public string DisplayNumber => string.IsNullOrEmpty(CadastralNumber) ? IsPdfDocument ? Path.GetFileName(EntryName) : IsNotice ? "Уведомление об отсутствии сведений" : "Без кадастрового номера" : CadastralNumber;
    public string Subtitle => string.IsNullOrEmpty(Address) ? Path.GetFileName(EntryName) : Address;
    public string SignatureStatus => Attachments.Any(a => a.Extension is ".sig" or ".p7s")
        ? "Файл подписи найден · подлинность не проверена" : "Связанная подпись не найдена";
    public string GeometryStatus => Rings.Count > 0 ? $"{Rings.Count} контур(ов) · {Rings.Sum(r => r.Points.Count)} точек"
        : Attachments.Any(a => a.IsImage || a.IsPdf) ? "Есть графические вложения" : "Графика и координаты не найдены";
}
public sealed record ImportResult(IReadOnlyList<ExtractDocument> Documents, IReadOnlyList<string> Errors);
