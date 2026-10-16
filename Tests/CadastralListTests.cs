using AngleSharp.Html.Parser;
using RosreestrViewer.Core;

internal static class CadastralListTests
{
    public static void Run(Action<string, Action> check)
    {
        const string a = "50:21:0010203:456", b = "77:01:0001001:1234", c = "78:01:0000001:100";
        static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
        static string Section(string html, string title, int count)
        {
            var document = new HtmlParser().ParseDocument(html);
            var heading = document.QuerySelectorAll("h2").SingleOrDefault(h => h.TextContent == $"{title} · {count}");
            Assert(heading is not null, $"Missing heading/count: {title} · {count}");
            return heading!.NextElementSibling!.TextContent;
        }
        check("Cadastral lists extract sorted unique numbers from arbitrary prose", () =>
        {
            var numbers = RegistryService.CadastralNumbers($"Дата 15.10.2026; площадь 1200 м². Квартира ({b}), участок {a}; повтор: {a}.");
            Assert(numbers.SequenceEqual(new[] { a, b }), "Lost, invented or duplicated cadastral number");
        });
        check("Cadastral lists normalize spaces, NBSP, tabs and line breaks", () =>
            Assert(RegistryService.CadastralNumbers("50\u00a0:\t21 :\r\n0010203:\n456").SequenceEqual(new[] { a }), "Whitespace split a number"));
        check("Malformed numeric chains do not yield cadastral substrings", () =>
        {
            string[] malformed = ["150:21:0010203:456", "50:121:0010203:456", "50:21:00102030:456",
                "50:21:0010203:456:789", "12 : 50 : 21 : 0010203 : 456", "50:21:0010203:456 :", "50:21:0010203:"];
            foreach (var value in malformed) Assert(RegistryService.CadastralNumbers(value).Length == 0, "Accepted substring of " + value);
            Assert(RegistryService.CadastralNumbers($"{malformed[3]}; {b}").SequenceEqual(new[] { b }), "Malformed neighbor hid a valid number");
        });
        check("Cadastral lists preserve leading zeros and registration-number extraction", () =>
        {
            Assert(RegistryService.CadastralNumbers($"{a}; 50:21:0010203:0456").Length == 2, "Different identifiers merged");
            Assert(RegistryService.CadastralNumbers(a + "-50/001/2020-1").SequenceEqual(new[] { a }), "Registration suffix prevented extraction");
        });
        check("Cadastral comparison reports both differences and intersection with unique counts", () =>
        {
            var html = RegistryService.CompareCadastralLists($"{a}; {b}; {a}", $"{b}; {c}; {b}");
            Assert(Section(html, "Первый список", 2) == a + "\n" + b, "Wrong first list");
            Assert(Section(html, "Второй список", 2) == b + "\n" + c, "Wrong second list");
            Assert(Section(html, "Только в первом", 1) == a && Section(html, "Только во втором", 1) == c && Section(html, "Общие", 1) == b, "Set membership incorrect");
        });
        check("Identical and disjoint cadastral lists have explicit empty results", () =>
        {
            var same = RegistryService.CompareCadastralLists(a, a);
            Assert(Section(same, "Только в первом", 0) == "Нет номеров" && Section(same, "Только во втором", 0) == "Нет номеров" && Section(same, "Общие", 1) == a, "Identical lists incorrect");
            var disjoint = RegistryService.CompareCadastralLists(a, b);
            Assert(Section(disjoint, "Только в первом", 1) == a && Section(disjoint, "Только во втором", 1) == b && Section(disjoint, "Общие", 0) == "Нет номеров", "Disjoint lists incorrect");
        });
        check("Cadastral comparison supports either list empty and both without numbers", () =>
        {
            var left = RegistryService.CompareCadastralLists(a, "");
            var right = RegistryService.CompareCadastralLists("", b);
            Assert(Section(left, "Только в первом", 1) == a && Section(left, "Второй список", 0) == "Нет номеров", "Empty second list incorrect");
            Assert(Section(right, "Только во втором", 1) == b && Section(right, "Первый список", 0) == "Нет номеров", "Empty first list incorrect");
            var empty = RegistryService.CompareCadastralLists("Договор № 123 от 15.10.2026", "");
            foreach (string title in new[] { "Первый список", "Второй список", "Только в первом", "Только во втором", "Общие" })
                Assert(Section(empty, title, 0) == "Нет номеров", "Empty comparison incorrect");
        });
        check("Cadastral report contains extracted identifiers without pasted markup", () =>
        {
            var html = RegistryService.CompareCadastralLists($"<script>alert(1)</script><b>{a}</b>", "");
            Assert(!html.Contains("<script>") && !html.Contains("<b>") && Section(html, "Первый список", 1) == a, "Raw input appeared in report");
        });
    }
}
