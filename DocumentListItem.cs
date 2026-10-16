using RosreestrViewer.Core;

namespace RosreestrViewer;

/// <summary>Read-only XAML presentation boundary; immutable domain objects are not XAML-activated.</summary>
public sealed class DocumentListItem
{
    internal ExtractDocument Document { get; }
    public string DisplayNumber => Document.DisplayNumber;
    public string Subtitle => Document.Subtitle;
    public string Kind => Document.Kind;
    public DocumentListItem(ExtractDocument document) => Document = document;
    public override string ToString() => DisplayNumber + " · " + Kind + " · " + Document.EntryName;
}
