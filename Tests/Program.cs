using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using RosreestrViewer.Core;

if (args.Length > 1 && args[0] == "--audit") return await CorpusAudit.RunAsync(args[1]);
if (args.Length > 1 && args[0] == "--forms") return await PrintFormAudit.Run(args[1], Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Templates")));
if (args.Length == 4 && args[0] == "--validate")
{
    var validation = SchemaValidationService.Validate(File.ReadAllBytes(args[3]), args[1], args[2]);
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { valid = validation.Valid, messages = validation.Messages.Count,
        rootSchemaMismatch = validation.Messages.Any(m => m.StartsWith("Выбранная XSD не описывает", StringComparison.Ordinal)) }));
    return 0;
}
if (args.Length > 1 && args[0] == "--probe")
{
    var batch = await new ImportService().ImportAsync(File.Exists(args[1]) ? [args[1]] : Directory.EnumerateFiles(args[1], "*.xml"));
    foreach (var group in batch.Documents.GroupBy(d => d.RootName)) Console.WriteLine($"{group.Key}: {group.Count()} objects, {group.Count(d => d.Rings.Count > 0)} with geometry, {group.Count(d => d.CadastralNumber.Length > 0)} with primary cadastral number.");
    return batch.Errors.Count == 0 ? 0 : 1;
}

var results = new List<(string Name, bool Pass, string Detail)>();
void Check(string name, Action test) { try { test(); results.Add((name, true, "")); } catch (Exception e) { results.Add((name, false, e.Message)); } }
void Assert(bool condition, string detail) { if (!condition) throw new Exception(detail); }
void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Input was not rejected"); }
ExtractDocument Parse(string xml) => ExtractParser.Parse(Encoding.UTF8.GetBytes(xml), "synthetic.xml", "synthetic.xml").Single();
string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
var sample = ExtractParser.Parse(File.ReadAllBytes(Path.Combine(root, "Samples", "01-land.xml")), "sample.xml", "sample.xml").Single();
var sample2 = ExtractParser.Parse(File.ReadAllBytes(Path.Combine(root, "Samples", "02-land-updated.xml")), "sample2.xml", "sample2.xml").Single();
PrintFormTests.Run(root, Check);
CadastralListTests.Run(Check);
var room = ExtractParser.Parse(File.ReadAllBytes(Path.Combine(root, "Samples", "03-room.xml")), "room.xml", "room.xml").Single();
Check("Modern cadastral number and address", () => Assert(sample.CadastralNumber == "50:21:0010203:456" && sample.Address.Contains("Лесная"), "Wrong object data"));
Check("Modern area and issuance date", () => Assert(sample.Area == "1200" && sample.Date == "2026-09-12", "Wrong area/date"));
Check("Holder full name", () => Assert(sample.Rights.Single().Holder == "Примеров Алексей Иванович", "Full name lost"));
Check("Share numerator and denominator", () => Assert(room.Rights.Single().Share == "1/2", "Wrong share"));
Check("Restricted owner data preserved", () => Assert(room.Rights.Single().Holder.Contains("не предоставлены"), "Restricted holder replaced"));
Check("Absent geometry stays absent", () => Assert(room.Rings.Count == 0 && GeometryRenderer.Html(room).Contains("Нет координат"), "Geometry invented"));
Check("Stable coordinate calculation", () => Assert(GeometryRenderer.Area(sample.Rings.Single()) == 1200 && GeometryRenderer.Length(sample.Rings.Single()) == 140, "Wrong area/perimeter"));
Check("Open contour area not invented", () => { var ring = new BoundaryRing("line", "", [new(0, 0, "1", ""), new(5, 0, "2", ""), new(5, 5, "3", "")]); Assert(!GeometryRenderer.Closed(ring) && GeometryRenderer.Area(ring) == 0, "Open polygon closed implicitly"); });
Check("Unknown coordinate systems kept separate for display and export", () => { var ring = new BoundaryRing("line", "", [new(0, 0, "1", ""), new(5, 0, "2", "")]); Assert(GeometryRenderer.Systems([ring, ring]).Count == 2, "Unidentified systems merged"); });
Check("SHA256 original bytes", () => Assert(sample.Hash == Convert.ToHexString(SHA256.HashData(sample.OriginalBytes)), "Original bytes changed"));
Check("No verified signature claim", () => Assert(sample.SignatureStatus.Contains("не найдена"), "Signature status wrong"));
Check("Difference includes changed area and mortgage", () => { var html = RegistryService.Compare(sample, sample2); Assert(html.Contains("1205") && html.Contains("Ипотека"), "Difference lost"); });
Check("Duplicate cad numbers extracted once", () => Assert(RegistryService.CadastralNumbers("50 : 21 : 0010203 : 456, 50:21:0010203:456; 77:01:0001001:1234").Length == 2, "Deduplication failed"));
Check("Unknown namespace prefix works", () => Assert(Parse("<r:extract_base_params_land xmlns:r='urn:test'><r:land_record><r:cad_number>1:2:3:4</r:cad_number><r:area><r:value>10</r:value></r:area></r:land_record></r:extract_base_params_land>").Area == "10", "Namespace prefix handling failed"));
Check("Unknown family fallback and original fields", () => { var d = Parse("<future><new_field>preserved</new_field></future>"); Assert(!d.KnownFamily && d.Fields.Single().Value == "preserved", "Unknown field lost"); });
Check("Multiple objects have isolated characteristics", () => { var ds = ExtractParser.Parse(Encoding.UTF8.GetBytes("<extract_cadastral_plan_territory><land_record><cad_number>1:1:1:1</cad_number><area>10</area></land_record><land_record><cad_number>1:1:1:2</cad_number><area>20</area></land_record></extract_cadastral_plan_territory>"), "x", "x"); Assert(ds.Count == 2 && ds[0].Area == "10" && ds[1].Area == "20", "Objects merged"); });
Check("Legacy ordinates in attributes", () => { var d = Parse("<KVZU><Parcel CadastralNumber='1:2:3:4'><EntitySpatial EntSys='МСК'><SpatialElement><SpelementUnit SuNmb='1'><Ordinate X='1.2' Y='3,4' /></SpelementUnit><SpelementUnit SuNmb='2'><Ordinate X='2' Y='4' /></SpelementUnit></SpatialElement></EntitySpatial></Parcel></KVZU>"); Assert(d.CadastralNumber == "1:2:3:4" && d.Rings.Single().Points[0].Y == 3.4, "Legacy ordinate failed"); });
Check("Nested dictionary type value", () => { var d = Parse("<extract><right_record><right_type><code>001</code><value>Собственность</value></right_type></right_record></extract>"); Assert(d.Rights.Single().Type == "Собственность", "Dictionary label lost"); });
Check("HTML escaped from XML", () => { var d = Parse("<future><address><note>&lt;script&gt;alert(1)&lt;/script&gt;</note></address></future>"); var html = ReportRenderer.Card(d); Assert(!html.Contains("<script>") && html.Contains("&lt;script&gt;"), "Unsafe markup" ); });
Check("DTD rejected", () => Reject<XmlException>(() => Parse("<!DOCTYPE x [<!ENTITY secret SYSTEM 'file:///C:/Windows/win.ini'>]><x>&secret;</x>")));
Check("Malformed XML rejected", () => Reject<XmlException>(() => Parse("<extract><x></extract>")));
Check("Path traversal and absolute paths rejected", () => { foreach (var p in new[] { "../secret.png", "C:\\secret.png", "/secret.png", "a/../../secret.png" }) Reject<InvalidDataException>(() => ImportService.NormalizeEntry(p)); });
Check("Unofficial domain rejected", () => Assert(!GraphicDownloadService.Allowed(new Uri("https://rosreestr.gov.ru.evil.example/image.png")) && !GraphicDownloadService.Allowed(new Uri("http://rosreestr.gov.ru/image.png")) && GraphicDownloadService.Allowed(new Uri("https://portal.rosreestr.gov.ru/image.png")), "Domain restriction failed"));
Check("Stream size limited", () => Reject<InvalidDataException>(() => ImportService.ReadBounded(new MemoryStream(new byte[20]), 10)));
Check("Graphic content detected without file extension", () => Assert(GraphicDownloadService.DetectExtension("%PDF-1.7"u8.ToArray()) == ".pdf" && GraphicDownloadService.DetectExtension("<html>error</html>"u8.ToArray()) == "", "HTML accepted as graphic"));
Check("Internal server plan path identified", () => { var d = Parse("<extract_about_property_room><plans><plan><file_link>/10.0.0.1/archive/plan.jpg</file_link><file_ref><file_storage_path><path>id/id/EXT_FILE</path></file_storage_path></file_ref></plan></plans></extract_about_property_room>"); Assert(d.Attachments.Count == 2 && d.Attachments.All(a => a.IsServerReference) && !d.Attachments.Any(a => a.IsRemote), "Internal path treated as public download"); });
Check("Legacy plan filename attribute preserved", () => Assert(Parse("<KPOKS><Plans><Plan Name='drawing.jpg'/></Plans></KPOKS>").Attachments.Single().Reference == "drawing.jpg", "Plan @Name lost"));
Check("Known absence notice has distinct presentation", () => { var d = Parse("<exract_notice_absence_request_info_12><reasons_notification>Нет сведений</reasons_notification></exract_notice_absence_request_info_12>"); Assert(d.IsNotice && d.NoticeReason == "Нет сведений" && d.KnownFamily, "Notice not recognized"); });
Check("Legacy GKN object attributes override embedded rights shell", () => { var d = Parse("<KPOKS><Realty><Flat CadastralNumber='77:1:2:3'><Area Value='30.2'/></Flat></Realty><eDocument><ExtractObject><ObjectRight><Right><Registration><RegNumber>test</RegNumber></Registration></Right></ObjectRight></ExtractObject></eDocument></KPOKS>"); Assert(d.CadastralNumber == "77:1:2:3" && d.Area == "30.2" && d.Rights.Count == 1, "GKN outer object not read"); });
Check("Repeated records preserve holder association", () => { var a = Parse("<extract><right_record><right_number>1</right_number><right_holder><name>A</name></right_holder></right_record><right_record><right_number>2</right_number><right_holder><name>B</name></right_holder></right_record></extract>"); var b = Parse("<extract><right_record><right_number>1</right_number><right_holder><name>B</name></right_holder></right_record><right_record><right_number>2</right_number><right_holder><name>A</name></right_holder></right_record></extract>"); Assert(!RegistryService.Compare(a,b).Contains("Изменённых путей: 0"), "Association swap invisible"); });
Check("CSV formula neutralization", () => { var d = Parse("<extract><address><note>=HYPERLINK(1)</note></address></extract>"); Assert(RegistryService.Csv([d]).Contains("'=HYPERLINK"), "Spreadsheet formula injected"); });
Check("Windows1251 XML honored", () => { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); var bytes = Encoding.GetEncoding(1251).GetBytes("<?xml version='1.0' encoding='windows-1251'?><Extract><ObjectTypeText>Помещение</ObjectTypeText></Extract>"); Assert(ExtractParser.Parse(bytes, "cp1251", "cp1251").Single().Kind == "Помещение", "Legacy encoding failed"); });
var temp = Path.Combine(Path.GetTempPath(), "RosreestrViewerTests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temp);
try
{
    var xml = Path.Combine(temp, "document.xml"); File.WriteAllBytes(xml, sample.OriginalBytes); File.WriteAllBytes(xml + ".sig", [1, 2, 3]);
    var imported = await new ImportService().ImportAsync([xml]);
    var schema = Path.Combine(temp, "simple.xsd"); File.WriteAllText(schema, "<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'><xs:element name='root' type='xs:int'/></xs:schema>");
    Check("XSD accepts conforming root", () => Assert(SchemaValidationService.Validate("<root>12</root>"u8.ToArray(), schema).Valid, "Valid XML rejected"));
    Check("XSD rejects invalid value and different root", () => Assert(!SchemaValidationService.Validate("<root>bad</root>"u8.ToArray(), schema).Valid && !SchemaValidationService.Validate("<other>12</other>"u8.ToArray(), schema).Valid, "Invalid XML reported as valid"));
    Directory.CreateDirectory(Path.Combine(temp, "main")); Directory.CreateDirectory(Path.Combine(temp, "common"));
    var mainSchema = Path.Combine(temp, "main", "root.xsd");
    File.WriteAllText(mainSchema, "<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'><xs:include schemaLocation='../common/types.xsd'/><xs:element name='root' type='Number'/></xs:schema>");
    File.WriteAllText(Path.Combine(temp, "common", "types.xsd"), "<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'><xs:simpleType name='Number'><xs:restriction base='xs:int'/></xs:simpleType></xs:schema>");
    Check("XSD relative dependencies work inside selected bundle", () => Assert(SchemaValidationService.Validate("<root>12</root>"u8.ToArray(), mainSchema, temp).Valid, "Sibling SchemaCommon dependency blocked"));
    Check("XSD dependency outside selected root not reported valid", () => Assert(!SchemaValidationService.Validate("<root>12</root>"u8.ToArray(), mainSchema).Valid, "Outside dependency accepted"));
    var pdfFile = Path.Combine(temp, "original.pdf"); File.WriteAllBytes(pdfFile, "%PDF-1.7\nminimal test fixture"u8.ToArray());
    var pdfImport = await new ImportService().ImportAsync([pdfFile]);
    Check("Standalone PDF retained without pretending XML data", () => Assert(pdfImport.Documents.Single().IsPdfDocument && pdfImport.Documents.Single().OriginalBytes.SequenceEqual(File.ReadAllBytes(pdfFile)), "PDF original lost"));
    File.WriteAllBytes(Path.ChangeExtension(xml, ".pdf"), "%PDF-1.7\npaired synthetic fixture"u8.ToArray());
    var paired = await new ImportService().ImportAsync([xml]);
    Check("PDF sibling paired and original bytes preserved", () => Assert(paired.Documents.Single().DocumentPdfs.Single().Content!.SequenceEqual(File.ReadAllBytes(Path.ChangeExtension(xml, ".pdf"))), "Paired PDF changed or missing"));
    File.WriteAllBytes(xml + ".pdf", "%PDF-1.7\nsecond distinct version"u8.ToArray());
    var twoPdfs = await new ImportService().ImportAsync([xml]);
    Check("Two distinct sibling PDFs remain separate choices", () => Assert(twoPdfs.Documents.Single().DocumentPdfs.Count == 2, "Ambiguous PDFs collapsed"));
    File.WriteAllBytes(xml + ".pdf", "not a PDF"u8.ToArray());
    var badPdf = await new ImportService().ImportAsync([xml]);
    Check("Invalid PDF sibling does not discard XML", () => Assert(badPdf.Documents.Count == 1 && badPdf.Documents.Single().DocumentPdfs.Count == 1 && badPdf.Documents.Single().Warnings.Any(w => w.Contains("неверный заголовок")), "Invalid sibling lost document or was accepted"));
    Check("Local SIG paired by exact document filename", () => Assert(imported.Documents.Single().SignatureStatus.Contains("не проверена"), "SIG treated as verified or not paired"));
    var archive = Path.Combine(temp, "bundle.zip");
    using (var z = ZipFile.Open(archive, ZipArchiveMode.Create))
    {
        using (var s = z.CreateEntry("folder/document.xml").Open()) s.Write(sample.OriginalBytes);
        using (var s = z.CreateEntry("folder/document.xml.sig").Open()) s.Write([1, 2, 3]);
        using (var s = z.CreateEntry("folder/document.pdf").Open()) s.Write("%PDF-1.7\nZIP paired fixture"u8);
        using (var s = z.CreateEntry("bad.xml").Open()) s.Write(Encoding.UTF8.GetBytes("<broken>"));
    }
    var zipResult = await new ImportService().ImportAsync([archive]);
    Check("ZIP valid documents survive malformed neighbor", () => Assert(zipResult.Documents.Count == 1 && zipResult.Errors.Count == 1 && zipResult.Documents.Single().Attachments.Any(a => a.Extension == ".sig"), "ZIP partial import failed"));
    Check("PDF in ZIP matched to XML in same directory", () => Assert(zipResult.Documents.Single().DocumentPdfs.Single().Reference == "folder/document.pdf", "ZIP PDF not paired"));
    var mixedArchive = Path.Combine(temp, "mixed-pdfs.zip");
    using (var z = ZipFile.Open(mixedArchive, ZipArchiveMode.Create))
    {
        using (var s = z.CreateEntry("document.xml").Open()) s.Write(sample.OriginalBytes);
        using (var s = z.CreateEntry("another-name.pdf").Open()) s.Write("%PDF-1.7\nstandalone package PDF"u8);
    }
    var mixed = await new ImportService().ImportAsync([mixedArchive]);
    Check("Unpaired PDF in XML ZIP remains an independent document", () => Assert(mixed.Documents.Count == 2 && mixed.Documents.Count(d => d.IsPdfDocument) == 1 && mixed.Documents.First(d => !d.IsPdfDocument).DocumentPdfs.Count == 0, "Unpaired PDF hidden or wrongly associated"));
    var traversal = Path.Combine(temp, "traversal.zip"); using (var z = ZipFile.Open(traversal, ZipArchiveMode.Create)) z.CreateEntry("../outside.xml");
    var rejected = await new ImportService().ImportAsync([traversal]);
    Check("Unsafe ZIP rejected without extraction", () => Assert(rejected.Documents.Count == 0 && rejected.Errors.Count == 1, "Unsafe archive accepted"));
    var canceled = new CancellationTokenSource(); canceled.Cancel();
    bool cancelPassed = false; try { await new ImportService().ImportAsync([xml], canceled.Token); } catch (OperationCanceledException) { cancelPassed = true; }
    Check("Canceled import stops", () => Assert(cancelPassed, "Cancellation ignored"));
    if (args.Length > 0 && Directory.Exists(args[0]))
    {
        var legacy = await new ImportService().ImportAsync(Directory.EnumerateFiles(args[0], "*.xml"));
        Check("Real legacy corpus including absence notices", () => Assert(legacy.Errors.Count == 0 && legacy.Documents.Count == 33 && legacy.Documents.Count(d => d.CadastralNumber.Length > 0) == 18 && legacy.Documents.All(d => d.Fields.Count > 0), $"Documents={legacy.Documents.Count}; errors={legacy.Errors.Count}"));
        Console.WriteLine($"Legacy corpus: {legacy.Documents.Count} object records; {legacy.Documents.Count(d => d.Rights.Count > 0)} with rights; no personal data exported.");
    }
}
finally { Directory.Delete(temp, true); }
foreach (var r in results) Console.WriteLine($"{(r.Pass ? "PASS" : "FAIL")} {r.Name}{(r.Detail.Length == 0 ? "" : ": " + r.Detail)}");
Console.WriteLine($"Passed: {results.Count(r => r.Pass)}; Failed: {results.Count(r => !r.Pass)}");
return results.All(r => r.Pass) ? 0 : 1;
