using System.Xml;
using System.Xml.Schema;

namespace RosreestrViewer.Core;

public sealed record SchemaValidationResult(bool Valid, IReadOnlyList<string> Messages);
public static class SchemaValidationService
{
    public static SchemaValidationResult Validate(byte[] xml, string schemaPath, string? bundleFolder = null)
    {
        var messages = new List<string>(); bool failed = false;
        void Error(object? sender, ValidationEventArgs args)
        { messages.Add(args.Severity + ": " + args.Message); failed = true; }
        var rootFolder = Path.GetFullPath(bundleFolder ?? Path.GetDirectoryName(Path.GetFullPath(schemaPath))!);
        if (!Path.GetFullPath(schemaPath).StartsWith(rootFolder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Выбранная XSD должна находиться внутри корневой папки пакета.");
        var resolver = new BundleResolver(rootFolder);
        var schemas = new XmlSchemaSet { XmlResolver = resolver };
        schemas.ValidationEventHandler += Error;
        using (var r = XmlReader.Create(schemaPath, ExtractParser.ReaderSettings())) schemas.Add(null, r);
        schemas.Compile();
        var settings = ExtractParser.ReaderSettings();
        settings.Schemas = schemas; settings.ValidationType = ValidationType.Schema;
        settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;
        settings.ValidationEventHandler += Error;
        using var source = new MemoryStream(xml);
        using var reader = XmlReader.Create(source, settings);
        reader.MoveToContent();
        if (!schemas.GlobalElements.Contains(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI)))
            return new SchemaValidationResult(false, ["Выбранная XSD не описывает корневой элемент и пространство имён этого XML."]);
        while (reader.Read()) { }
        if (messages.Count == 0) messages.Add("XML соответствует выбранной локальной XSD. Проверка подписи и происхождения схемы не выполнялась.");
        return new SchemaValidationResult(!failed, messages);
    }
    sealed class BundleResolver(string folder) : XmlResolver
    {
        public override System.Net.ICredentials? Credentials { set { } }
        public override object GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
        {
            string prefix = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!absoluteUri.IsFile || !Path.GetFullPath(absoluteUri.LocalPath).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new XmlException("Зависимости XSD разрешены только внутри выбранного пакета схем; сетевые ссылки запрещены.");
            if (new FileInfo(absoluteUri.LocalPath).Length > ExtractParser.MaxXmlBytes) throw new XmlException("XSD превышает 32 МБ.");
            return File.OpenRead(absoluteUri.LocalPath);
        }
    }
}
