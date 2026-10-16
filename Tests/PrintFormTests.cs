using System.Text;
using System.Text.Json.Nodes;
using AngleSharp.Html.Parser;
using RosreestrViewer.Core;

internal static class PrintFormTests
{
    public static void Run(string project, Action<string, Action> check)
    {
        static void Assert(bool value, string why) { if (!value) throw new Exception(why); }
        var service = new PrintFormService(Path.Combine(project, "Templates"));
        ExtractDocument Parse(string xml) => ExtractParser.Parse(Encoding.UTF8.GetBytes(xml), "synthetic.xml", "synthetic.xml").Single();
        var legacy = ExtractParser.Parse(File.ReadAllBytes(Path.Combine(project, "Samples", "04-print-form.xml")), "synthetic.xml", "synthetic.xml").Single();
        check("Archived XSL preserves table layout, headings and both registrations", () =>
        {
            var result = service.Render(legacy);
            Assert(result.Available, result.Reason);
            using var page = new HtmlParser().ParseDocument(result.Html);
            Assert(result.Template?.Id == "egrp-list-07" && page.QuerySelectorAll("table").Length >= 3, "Wrong template or tables lost");
            Assert(page.QuerySelector("td[rowspan]") != null && page.QuerySelector("td[colspan]") != null, "Merged cells lost");
            Assert(page.QuerySelector("td[rowspan='0']") == null, "IE8 zero rowspan breaks the HTML5 table grid");
            Assert(page.Body!.TextContent.Contains("Примеров Алексей") && page.Body.TextContent.Contains("Демонстрационный новый владелец"), "Repeated rights lost");
            Assert(page.QuerySelector("script,iframe,object,base") == null && result.Html.Contains("Content-Security-Policy"), "Active content allowed");
        });
        check("HTTP stylesheet instruction uses pinned HTTPS snapshot without network", () =>
            Assert(service.Render(Parse(Encoding.UTF8.GetString(legacy.OriginalBytes).Replace("https://portal", "http://portal"))).Available, "Old HTTP link not mapped"));
        check("Unknown stylesheet is not silently replaced", () =>
            Assert(!service.Render(Parse(Encoding.UTF8.GetString(legacy.OriginalBytes).Replace("Reestr_Extract_List/07/Common.xsl", "future/99/Common.xsl"))).Available, "Unknown XSL ignored"));
        check("Wrong XML version or namespace is not rendered through old XSL", () =>
        {
            Assert(!service.Render(Parse(Encoding.UTF8.GetString(legacy.OriginalBytes).Replace("Version=\"07\"", "Version=\"99\""))).Available, "Wrong version accepted");
            Assert(!service.Render(Parse("<extract_about_property_room xmlns='urn:future'><room_record><params><area>123</area></params></room_record></extract_about_property_room>")).Available, "Wrong namespace accepted");
        });
        check("Third party archived stylesheet is labelled and resolves all includes offline", () =>
        {
            var result = service.Render(Parse("<extract_about_property_room><room_record><object><common_data><cad_number>1:1:1:1</cad_number></common_data></object><params><area>123.5</area><name>Демонстрационное помещение</name></params></room_record></extract_about_property_room>"));
            Assert(result.Available && result.Html.Contains("123.5") && result.Template!.Origin == "ThirdPartyArchive" && result.Warnings.Count > 0, result.Reason);
        });
        foreach (var kind in new[] { ("KPOKS", "kpoks/4.0.1"), ("KVOKS", "kvoks/3.0.1"), ("KVZU", "kvzu/7.0.1") })
            check("Cadastral stylesheet compiles with local entities and dictionaries: " + kind.Item1, () =>
            {
                var result = service.Render(Parse($"<{kind.Item1} xmlns='urn://x-artefacts-rosreestr-ru/outgoing/{kind.Item2}'><Object/></{kind.Item1}>"));
                // An empty extract may not yield a form, but compile failures are always errors.
                Assert(!result.Reason.StartsWith("Печатная форма не сформирована:"), result.Reason);
            });
        check("Source XML bytes remain unchanged after XSLT", () =>
        { var bytes = legacy.OriginalBytes.ToArray(); service.Render(legacy); Assert(legacy.OriginalBytes.SequenceEqual(bytes), "Source modified"); });
        var temporary = Path.Combine(Path.GetTempPath(), "RosreestrPrintTests-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temporary);
        try
        {
            // A tiny catalog proves that document() cannot leave the pinned resource set.
            var xsl = "<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0'><xsl:template match='/'><html><body><table><tr><td><xsl:value-of select=\"document('https://example.com/secret.xml')\"/></td></tr></table></body></html></xsl:template></xsl:stylesheet>";
            File.WriteAllText(Path.Combine(temporary, "test.xsl"), xsl);
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(temporary, "test.xsl"))));
            var catalog = new JsonObject { ["version"] = 1, ["resources"] = new JsonArray(new JsonObject { ["uri"] = "https://portal.rosreestr.ru/test.xsl", ["file"] = "test.xsl", ["sha256"] = hash }),
                ["forms"] = new JsonArray(new JsonObject { ["id"] = "test", ["root"] = "root", ["namespace"] = "", ["version"] = "", ["source"] = "https://portal.rosreestr.ru/test.xsl", ["origin"] = "RosreestrArchive" }) };
            File.WriteAllText(Path.Combine(temporary, "catalog.json"), catalog.ToJsonString());
            check("XSL document() cannot access unlisted URLs", () => Assert(!new PrintFormService(temporary).Render(Parse("<root/>")).Available, "Network access allowed"));
            File.AppendAllText(Path.Combine(temporary, "test.xsl"), " ");
            check("Modified stylesheet checksum rejected before execution", () =>
            { try { _ = new PrintFormService(temporary); } catch (InvalidDataException) { return; } throw new Exception("Modified template trusted"); });
        }
        finally { Directory.Delete(temporary, true); }
    }
}
