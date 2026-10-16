using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace RosreestrViewer.Core;

/// <summary>Reads known semantic nodes, preserving every original field for unsupported revisions.</summary>
public static class ExtractParser
{
    public const int MaxXmlBytes = 32 * 1024 * 1024;
    static ExtractParser() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    static string N(XElement e) => e.Name.LocalName.ToLowerInvariant();
    static bool Named(XElement e, params string[] names) => names.Contains(N(e), StringComparer.OrdinalIgnoreCase);
    static IEnumerable<XElement> Nodes(XElement e, params string[] names) => e.DescendantsAndSelf().Where(x => Named(x, names));
    static string Value(XElement e, params string[] names)
    {
        foreach (var name in names)
        {
            var nodes = Nodes(e, name).ToArray();
            var leaf = nodes.FirstOrDefault(x => !x.HasElements);
            if (leaf != null) return leaf.Value.Trim();
            foreach (var node in nodes)
            {
                var text = node.Descendants().FirstOrDefault(x => !x.HasElements && Named(x, "value", "content", "name"));
                if (text != null) return text.Value.Trim();
            }
        }
        return "";
    }
    static string Attr(XElement e, params string[] names) => e.Attributes().FirstOrDefault(a => names.Contains(a.Name.LocalName, StringComparer.OrdinalIgnoreCase))?.Value.Trim() ?? "";
    static string Or(params string[] values) => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "";
    public static XmlReaderSettings ReaderSettings() => new()
    {
        DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null,
        MaxCharactersInDocument = MaxXmlBytes, IgnoreComments = true
    };
    public static IReadOnlyList<ExtractDocument> Parse(byte[] bytes, string source, string entryName)
    {
        if (bytes.Length > MaxXmlBytes) throw new InvalidDataException("XML превышает лимит 32 МБ.");
        using var stream = new MemoryStream(bytes, false);
        using var reader = XmlReader.Create(stream, ReaderSettings());
        var xml = XDocument.Load(reader, LoadOptions.None);
        var root = xml.Root ?? throw new InvalidDataException("XML не содержит корневого элемента.");
        var rootName = N(root);
        bool known = rootName.StartsWith("extract_about_property_", StringComparison.Ordinal)
            || rootName.StartsWith("extract_base_params_", StringComparison.Ordinal)
            || rootName is "extract_transfer_rights_property" or "extract_cadastral_plan_territory" or "exract_notice_absence_request_info_12" or "extract_notice_absence_request_info" or "extract" or "kpoks" or "kvoks" or "kpt" or "kpoz" or "kvzu" or "kuzem" or "egrn" or "reestr_extract";
        bool notice = rootName.Contains("notice_absence") || Nodes(root, "noticeobj").Any();
        var recordNames = new[] { "land_record", "build_record", "room_record", "construction_record", "car_parking_space_record", "under_construction_record", "extractobject", "parcel" };
        var candidates = root.DescendantsAndSelf().Where(x => Named(x, recordNames))
            .Where(x => !x.Ancestors().Any(a => Named(a, recordNames))).ToList();
        if (rootName is "kvoks" or "kpoks")
            candidates = Nodes(root, "realty").SelectMany(r => r.Elements().Where(x => Named(x, "building", "flat", "construction", "unfinished"))).ToList();
        if (candidates.Count == 0) candidates.Add(root);
        var allFields = Fields(root).ToArray();
        if (allFields.Length > 100_000) throw new InvalidDataException("XML содержит более 100 000 полей.");
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        return candidates.Select((record, index) =>
        {
            var context = candidates.Count == 1 ? root : record;
            var common = Nodes(record, "common_data", "objectdesc").FirstOrDefault() ?? record;
            string cad = Or(Attr(record, "CadastralNumber"), Value(common, "cad_number", "cadastral_number", "cadastralnumber"), Value(record, "cad_number", "cadastral_number", "cadastralnumber"));
            if (cad.Length == 0) cad = record.Descendants().Select(x => Attr(x, "CadastralNumber")).FirstOrDefault(x => x.Length > 0) ?? "";
            var area = Nodes(record, "area").FirstOrDefault();
            var addressNode = Nodes(record, "address", "address_readable").FirstOrDefault();
            var address = addressNode == null ? "" : Or(Value(addressNode, "readable_address", "address_readable", "note", "content"), AddressParts(addressNode));
            var doc = new ExtractDocument
            {
                SourcePath = source, EntryName = entryName, OriginalBytes = bytes, Hash = hash,
                RootName = root.Name.LocalName, Namespace = root.Name.NamespaceName,
                SchemaVersion = Or(Attr(root, "Version", "version"), root.Descendants().Select(x => Attr(x, "Version")).FirstOrDefault(x => x.Length > 0) ?? ""),
                KnownFamily = known, CadastralNumber = cad, IsNotice = notice,
                NoticeReason = Value(root, "reasons_notification", "reason", "typeinfotext"),
                Kind = notice ? "Уведомление" : Kind(rootName, N(record), Value(common, "objecttypetext", "type")),
                Area = area == null ? Value(record, "area_value") : Or(Value(area, "value", "area"), Attr(area, "Value", "Area")),
                Address = address,
                Date = Value(root, "date_formation", "date_formed", "date_issuance", "extractdate"),
                Purpose = Or(Value(record, "purpose", "assignation_code_text", "permitted_use_established", "permitted_use", "category"), Value(record, "name")),
                Cost = Or(Value(Nodes(record, "cost", "cad_cost", "cadastral_cost", "cadastralcost").FirstOrDefault() ?? new XElement("empty"), "value", "cost", "cadastralcost"), Nodes(record, "cadastralcost").Select(c => Attr(c, "Value")).FirstOrDefault() ?? ""),
                Status = Or(Value(record, "status", "record_status"), Attr(record, "State")),
                Fields = candidates.Count == 1 ? allFields : Fields(record).ToArray(),
                Rights = ParseRights(context), Encumbrances = ParseEncumbrances(context), Rings = ParseRings(record)
            };
            foreach (var reference in References(context)) doc.Attachments.Add(new Attachment(Path.GetFileName(reference.Replace('\\', '/')), reference));
            if (doc.Attachments.Any(a => a.IsServerReference)) doc.Warnings.Add("В XML есть внутренний путь хранения плана; публичный адрес загрузки не указан. Для просмотра нужен приложенный графический файл или готовый PDF из официального результата.");
            if (!known) doc.Warnings.Add("Семейство XML не распознано. Карточка извлечена по известным именам полей; полные данные доступны во вкладке «Поля XML».");
            else doc.Warnings.Add("Карточка — локальное представление известных полей XML. Соответствие полной официальной форме и XSD не проверено.");
            if (!notice && doc.Rings.Count == 0) doc.Warnings.Add("Координаты контуров не найдены. Без них нельзя восстановить границы или план помещения.");
            if (doc.Rings.Any(r => string.IsNullOrWhiteSpace(r.CoordinateSystem))) doc.Warnings.Add("У части контуров не указана система координат. Привязка к карте и метрические расчёты не подтверждены.");
            if (candidates.Count > 1) doc.Warnings.Add("Документ содержит несколько объектов; общие разделы и подпись относятся к исходному XML. Несопоставленные сведения смотрите в исходнике.");
            return doc;
        }).ToArray();
    }
    static string Kind(string root, string record, string explicitKind)
    {
        var s = root + " " + record;
        if (s.Contains("land") || s.Contains("parcel") || s.Contains("kvzu") || s.Contains("kpoz")) return "Земельный участок";
        if (s.Contains("room") || s.Contains("kpoks") || record == "flat") return "Помещение";
        if (s.Contains("parking")) return "Машино-место";
        if (s.Contains("construction")) return "Незавершённое строительство";
        if (s.Contains("build") || s.Contains("kvoks")) return "Здание";
        if (explicitKind.Length > 0) return explicitKind;
        return root.Contains("transfer_rights") ? "Переход прав" : "Объект недвижимости";
    }
    static string AddressParts(XElement node) => string.Join(", ", node.DescendantsAndSelf().Where(x => !x.HasElements)
        .Select(x => x.Value.Trim()).Concat(node.DescendantsAndSelf().SelectMany(x => x.Attributes()).Where(a => a.Name.LocalName is "Name" or "Value" or "Type").Select(a => a.Value))
        .Where(x => x.Length > 0).Distinct());
    static RightEntry[] ParseRights(XElement context) => Nodes(context, "right_record", "registration", "right")
        .Where(x => !x.Ancestors().Any(a => Named(a, "right_record", "registration", "right")))
        .Select(x =>
        {
            var owners = x.Parent == null ? [] : Nodes(x.Parent, "owner").ToArray();
            var matchedOwner = owners.FirstOrDefault(o => Attr(o, "OwnerNumber").Length > 0 && Attr(o, "OwnerNumber") == Attr(x, "RegistrNumber"))
                ?? (owners.Length == 1 ? owners[0] : null);
            var holderNode = Nodes(x, "right_holders", "right_holder", "owner", "subject").FirstOrDefault()
                ?? (N(x) == "registration" ? matchedOwner : null) ?? x;
            var people = Nodes(holderNode, "individual", "person", "legal_entity", "organization", "public_formation").ToArray();
            var holder = people.Length > 0 ? string.Join("; ", people.Select(p => Named(p, "individual", "person")
                ? string.Join(" ", new[] { Value(p, "surname"), Value(p, "firstname", "first", "name"), Value(p, "patronymic", "patronym") }.Where(s => s.Length > 0)) : Value(p, "name", "content")))
                : Value(holderNode, "name", "content", "fio", "right_holder_name");
            return new RightEntry(holder.Length == 0 ? "Данные не предоставлены" : holder,
                Value(x, "right_type", "type"), Value(x, "right_number", "regnumber", "registration_number"),
                Value(x, "registration_date", "regdate", "date"), Or(Value(x, "sharetext", "share"), Share(x)));
        }).ToArray();
    static string Share(XElement e)
    {
        var s = Nodes(e, "share").FirstOrDefault();
        return s == null ? "" : Or(Attr(s, "Numerator") + (Attr(s, "Denominator").Length > 0 ? "/" + Attr(s, "Denominator") : ""),
            Value(s, "numerator") + (Value(s, "denominator").Length > 0 ? "/" + Value(s, "denominator") : ""));
    }
    static string[] ParseEncumbrances(XElement e) => Nodes(e, "restrict_record", "encumbrance", "restriction")
        .Where(x => !x.Ancestors().Any(a => Named(a, "restrict_record", "encumbrance", "restriction")))
        .Select(x => string.Join(" · ", new[] { Value(x, "type", "name"), Value(x, "restriction_number", "registration_number", "regnumber"), Value(x, "registration_date", "regdate"), Value(x, "content") }.Where(s => s.Length > 0)))
        .Where(x => x.Length > 0).ToArray();
    public static IReadOnlyList<BoundaryRing> ParseRings(XElement e)
    {
        var groups = Nodes(e, "spatial_element", "spatialelement").ToArray();
        var result = new List<BoundaryRing>();
        foreach (var group in groups)
        {
            var spatial = group.AncestorsAndSelf().FirstOrDefault(x => Named(x, "entity_spatial", "entityspatial")) ?? group;
            var system = Or(Value(spatial, "sk_id", "crs", "coordinate_system"), Attr(spatial, "EntSys", "CsCode", "SK_ID"));
            var points = new List<BoundaryPoint>();
            foreach (var node in Nodes(group, "ordinate"))
            {
                var xs = Or(Value(node, "x"), Attr(node, "X"));
                var ys = Or(Value(node, "y"), Attr(node, "Y"));
                if (!Number(xs, out var x) || !Number(ys, out var y)) continue;
                var unit = node.Parent ?? node;
                points.Add(new BoundaryPoint(x, y, Or(Value(node, "ord_nmb", "num_geopoint"), Attr(node, "OrdNmb", "NumGeopoint"), Attr(unit, "SuNmb"), (points.Count + 1).ToString(CultureInfo.InvariantCulture)), Or(Value(node, "delta_geopoint"), Attr(node, "DeltaGeopoint"))));
            }
            if (points.Count >= 2) result.Add(new BoundaryRing($"Контур {result.Count + 1}", system, points));
        }
        return result;
    }
    static bool Number(string s, out double n) => double.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out n) && double.IsFinite(n) && Math.Abs(n) < 1e10;
    static IEnumerable<string> References(XElement e) => e.DescendantsAndSelf().Where(x => !x.HasElements && Named(x, "file_name", "filename", "file", "image", "url", "link", "path", "plan", "file_link"))
        .Select(x => x.Value.Trim()).Concat(e.DescendantsAndSelf().SelectMany(x => x.Attributes()).Where(a => a.Name.LocalName.ToLowerInvariant() is "filename" or "file" or "href").Select(a => a.Value))
        .Concat(Nodes(e, "plan").Select(x => Attr(x, "Name")).Where(s => s.Length > 0))
        .Where(s => s.EndsWith("/EXT_FILE", StringComparison.OrdinalIgnoreCase) || (Uri.TryCreate(s, UriKind.Absolute, out var u) && u.Scheme is "http" or "https") || new[] { ".pdf", ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tif", ".tiff" }.Any(ext => s.EndsWith(ext, StringComparison.OrdinalIgnoreCase))).Distinct();
    static IEnumerable<XmlField> Fields(XElement root)
    {
        foreach (var node in root.DescendantsAndSelf())
        {
            if (node.Ancestors().Take(129).Count() > 128) throw new InvalidDataException("Вложенность XML превышает 128 уровней.");
            string path = string.Join("/", node.AncestorsAndSelf().Reverse().Select(x =>
            {
                if (x.Parent == null || x.Parent.Elements(x.Name).Take(2).Count() < 2) return x.Name.LocalName;
                int index = x.ElementsBeforeSelf(x.Name).Count() + 1;
                return x.Name.LocalName + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
            }));
            foreach (var a in node.Attributes().Where(a => !a.IsNamespaceDeclaration)) yield return new XmlField(path + "/@" + a.Name.LocalName, a.Name.LocalName, a.Value);
            if (!node.HasElements && !string.IsNullOrWhiteSpace(node.Value)) yield return new XmlField(path, node.Name.LocalName, node.Value.Trim());
        }
    }
}
