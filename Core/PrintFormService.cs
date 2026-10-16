using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace RosreestrViewer.Core;

public sealed class PrintFormTemplate
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Root { get; set; } = "";
    public string Namespace { get; set; } = "";
    public string Version { get; set; } = "";
    public string Source { get; set; } = "";
    public string Origin { get; set; } = "";
    public string[] Aliases { get; set; } = [];
    public string Provenance => Origin == "RosreestrArchive" ? "Архивный XSL Росреестра" : "Архивный сторонний XSL · legalcalc.ru";
}
public sealed record PrintFormResult(bool Available, string Html, PrintFormTemplate? Template, string Reason, IReadOnlyList<string> Warnings);

/// <summary>Transforms original XML through a versioned, offline, hash-checked XSL snapshot.</summary>
public sealed class PrintFormService
{
    public const int MaxOutputCharacters = 16 * 1024 * 1024;
    readonly TemplateResolver resolver;
    readonly PrintFormTemplate[] templates;
    readonly ConcurrentDictionary<string, Lazy<XslCompiledTransform>> compiled = new(StringComparer.Ordinal);
    public IReadOnlyList<PrintFormTemplate> Templates => templates;
    public PrintFormService(string templateFolder)
    {
        var catalogPath = Path.Combine(templateFolder, "catalog.json");
        if (new FileInfo(catalogPath).Length > 1024 * 1024) throw new InvalidDataException("Каталог XSL превышает лимит.");
        var catalog = JsonSerializer.Deserialize<TemplateCatalog>(File.ReadAllText(catalogPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("Каталог XSL пуст.");
        if (catalog.Version != 1 || catalog.Forms.Length > 100 || catalog.Resources.Length > 512) throw new InvalidDataException("Неизвестный формат каталога XSL.");
        templates = catalog.Forms;
        resolver = new TemplateResolver(templateFolder, catalog.Resources);
    }
    public PrintFormResult Render(ExtractDocument document)
    {
        if (document.IsPdfDocument) return Unavailable("Это самостоятельный PDF. Откройте «PDF из пакета».");
        try
        {
            using var source = new MemoryStream(document.OriginalBytes, false);
            using var reader = XmlReader.Create(source, ExtractParser.ReaderSettings());
            var xml = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
            var root = xml.Root!;
            string version = root.Attributes().FirstOrDefault(a => a.Name.LocalName.Equals("version", StringComparison.OrdinalIgnoreCase))?.Value
                ?? root.Elements().FirstOrDefault(e => e.Name.LocalName == "eDocument")?.Attribute("Version")?.Value ?? "";
            var candidates = templates.Where(t => t.Root == root.Name.LocalName && t.Namespace == root.Name.NamespaceName
                && (t.Version.Length == 0 || t.Version == version)).ToArray();
            bool compatibleAlias = false;
            var pi = xml.Nodes().OfType<XProcessingInstruction>().FirstOrDefault(p => p.Target == "xml-stylesheet");
            if (pi != null)
            {
                var href = Regex.Match(pi.Data, "\\bhref\\s*=\\s*(['\"])(.*?)\\1", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
                string uri = href.Success && Uri.TryCreate(href.Groups[2].Value, UriKind.Absolute, out var declared) ? TemplateResolver.Key(declared) : "";
                candidates = candidates.Where(t => TemplateResolver.Key(new Uri(t.Source)) == uri || t.Aliases.Any(a => TemplateResolver.Key(new Uri(a)) == uri)).ToArray();
                if (candidates.Length == 0) return Unavailable("В XML указан XSL, которого нет среди проверенных пакетов для этого формата. Произвольные шаблоны из интернета не загружаются.");
                compatibleAlias = candidates.Length == 1 && TemplateResolver.Key(new Uri(candidates[0].Source)) != uri;
            }
            if (candidates.Length != 1) return Unavailable($"Пакет печатной формы для {root.Name.LocalName}, namespace «{root.Name.NamespaceName}», версия «{version}» не найден. Доступны карточка, поля XML и исходник.");
            var template = candidates[0];
            var transform = compiled.GetOrAdd(template.Id, _ => new Lazy<XslCompiledTransform>(() => Compile(template))).Value;
            using var output = new LimitedWriter(MaxOutputCharacters);
            var settings = transform.OutputSettings!.Clone(); settings.CloseOutput = false;
            using (var writer = XmlWriter.Create(output, settings))
            {
                using var input = xml.CreateReader();
                transform.Transform(input, null, writer, resolver);
            }
            var result = PrepareHtml(output.ToString(), template, document);
            return compatibleAlias && result.Available ? result with { Warnings = result.Warnings.Append("Исходный XSL по ссылке XML не сохранился; выбран совместимый архивный шаблон для той же версии KPOKS. Точное совпадение с исходной формой не подтверждено.").ToArray() } : result;
        }
        catch (Exception ex) when (ex is XmlException or XsltException or IOException or InvalidDataException or ArgumentException)
        { return Unavailable("Печатная форма не сформирована: " + ex.Message); }
    }
    XslCompiledTransform Compile(PrintFormTemplate template)
    {
        var uri = new Uri(template.Source);
        using var stream = (Stream)resolver.GetEntity(uri, null, typeof(Stream));
        // Only the bundled, checksummed stylesheets may use small internal entities.
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse, XmlResolver = resolver,
            MaxCharactersFromEntities = 1024 * 1024, MaxCharactersInDocument = 4 * 1024 * 1024 };
        using var reader = XmlReader.Create(stream, settings, uri.AbsoluteUri);
        var transform = new XslCompiledTransform();
        transform.Load(reader, new XsltSettings(enableDocumentFunction: true, enableScript: false), resolver);
        return transform;
    }
    static PrintFormResult Unavailable(string reason) => new(false, "", null, reason, []);
    static PrintFormResult PrepareHtml(string html, PrintFormTemplate template, ExtractDocument source)
    {
        // Parse without a browsing context/resource loader. Preserve table layout and CSS,
        // while removing active legacy code and binding images only to imported attachments.
        using var page = new HtmlParser().ParseDocument(html);
        if (page.QuerySelector("table") == null || string.IsNullOrWhiteSpace(page.Body?.TextContent))
            return Unavailable("Шаблон не создал табличную форму для этого XML. Подмена документа кратким отчётом не выполняется.");
        var warnings = new List<string>();
        // IE8 treated a zero rowspan as one. HTML5 means "all remaining rows".
        // This archived template emits count(Registration)=0 inside Registration,
        // so leaving it unchanged shifts every later cell into a new column.
        if (template.Id == "egrp-list-07")
            foreach (var cell in page.QuerySelectorAll("td[rowspan='0'],th[rowspan='0']")) cell.SetAttribute("rowspan", "1");
        int scripts = page.QuerySelectorAll("script").Length;
        foreach (var element in page.QuerySelectorAll("script,iframe,frame,frameset,object,embed,base,link,meta,button,foreignObject").ToArray()) element.Remove();
        foreach (var input in page.QuerySelectorAll("input"))
        {
            if ((input.GetAttribute("type") ?? "text").ToLowerInvariant() is "button" or "submit" or "reset" or "file" or "image") input.Remove();
            else input.SetAttribute("disabled", "disabled");
        }
        foreach (var input in page.QuerySelectorAll("select,textarea")) input.SetAttribute("disabled", "disabled");
        foreach (var element in page.All)
        {
            foreach (var attribute in element.Attributes.ToArray().Where(a => a.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase)
                || a.Name is "srcset" or "srcdoc" or "action" or "formaction" || a.Name.Equals("href", StringComparison.OrdinalIgnoreCase) || a.Name.EndsWith(":href", StringComparison.OrdinalIgnoreCase)))
                element.RemoveAttribute(attribute.Name);
        }
        int unavailableImages = 0;
        foreach (var img in page.QuerySelectorAll("img"))
        {
            string reference = img.GetAttribute("src") ?? "";
            var assets = source.Attachments.Where(a => a.IsImage && a.Content != null && ReferenceMatches(a.Reference, reference)).ToArray();
            if (assets.Length == 1)
                img.SetAttribute("src", "data:" + ReportRenderer.Mime(assets[0].Extension) + ";base64," + Convert.ToBase64String(assets[0].Content!));
            else
            {
                img.RemoveAttribute("src"); img.SetAttribute("alt", "Графический файл не найден в полученном пакете");
                unavailableImages++;
            }
        }
        if (scripts > 0) warnings.Add("Сценарии старого браузера отключены. Интерактивная графика шаблона не воспроизводится; доступны вложения и отдельная схема по координатам.");
        if (unavailableImages > 0) warnings.Add($"Не найдены графические файлы формы: {unavailableImages}. Они не заменяются вымышленным изображением.");
        if (template.Origin != "RosreestrArchive") warnings.Add("Сторонний архивный шаблон. Полнота и совпадение с официальной формой этой версии не подтверждены.");
        if (source.RootName is "extract_about_property_build" or "extract_cadastral_plan_territory")
            warnings.Add("Форма относится ко всему исходному XML, включая связанные объекты, а не только к выбранной строке реестра.");
        page.DocumentElement.SetAttribute("lang", "ru");
        var meta = page.CreateElement("meta"); meta.SetAttribute("charset", "utf-8"); page.Head!.Prepend(meta);
        var csp = page.CreateElement("meta"); csp.SetAttribute("http-equiv", "Content-Security-Policy");
        csp.SetAttribute("content", "default-src 'none'; img-src data:; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'"); page.Head.Prepend(csp);
        var generator = page.CreateElement("meta"); generator.SetAttribute("name", "generator"); generator.SetAttribute("content", "Выписки ЕГРН; " + template.Provenance + "; " + template.Source); page.Head.Append(generator);
        var style = page.CreateElement("style");
        style.TextContent = "html{color-scheme:light}body{background:white;color:black}img{max-width:100%}@media screen{body{margin:24px}}@media print{@page{size:A4}body{margin:0}}";
        page.Head.Append(style);
        return new PrintFormResult(true, "<!doctype html>" + page.DocumentElement.OuterHtml, template, "", warnings);
    }
    static bool ReferenceMatches(string attachment, string image)
    {
        static string Normal(string s) => Uri.UnescapeDataString(s).Replace('\\', '/');
        return image.Length > 0 && Normal(attachment).Equals(Normal(image), StringComparison.OrdinalIgnoreCase);
    }
    sealed class TemplateCatalog
    {
        public int Version { get; set; }
        public PrintFormTemplate[] Forms { get; set; } = [];
        public TemplateResource[] Resources { get; set; } = [];
    }
    sealed class TemplateResource
    {
        public string Uri { get; set; } = "";
        public string File { get; set; } = "";
        public string Sha256 { get; set; } = "";
    }
    sealed class TemplateResolver : XmlResolver
    {
        readonly IReadOnlyDictionary<string, byte[]> resources;
        public TemplateResolver(string folder, IEnumerable<TemplateResource> entries)
        {
            var map = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
            {
                string path = ImportService.SafeLocalPath(folder, entry.File);
                if (!files.TryGetValue(path, out var bytes))
                {
                    if (new FileInfo(path).Length > 4 * 1024 * 1024) throw new InvalidDataException("Ресурс XSL превышает 4 МБ.");
                    bytes = File.ReadAllBytes(path); files.Add(path, bytes);
                }
                if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(entry.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Контрольная сумма ресурса XSL не совпадает: " + Path.GetFileName(path));
                if (!map.TryAdd(Key(new Uri(entry.Uri)), bytes)) throw new InvalidDataException("Дублирующийся адрес в каталоге XSL.");
            }
            resources = map;
        }
        public static string Key(Uri uri) => uri.Scheme is "http" or "https" && uri.IsDefaultPort && uri.Query.Length == 0 && uri.Fragment.Length == 0
            ? "https://" + uri.IdnHost.ToLowerInvariant() + uri.AbsolutePath : "";
        public override System.Net.ICredentials? Credentials { set { } }
        public override object GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
        {
            if (!resources.TryGetValue(Key(absoluteUri), out var bytes)) throw new XmlException("Зависимость отсутствует в локальном пакете XSL. Сеть и произвольные файлы недоступны.");
            return new MemoryStream(bytes, false);
        }
    }
    sealed class LimitedWriter(int limit) : StringWriter(CultureInfo.InvariantCulture)
    {
        int count;
        void Reserve(int length) { if ((long)count + length > limit) throw new InvalidDataException("Печатная форма превышает лимит 16 МБ."); count += length; }
        public override void Write(char value) { Reserve(1); base.Write(value); }
        public override void Write(string? value) { Reserve(value?.Length ?? 0); base.Write(value); }
        public override void Write(char[] buffer, int index, int count) { Reserve(count); base.Write(buffer, index, count); }
    }
}
