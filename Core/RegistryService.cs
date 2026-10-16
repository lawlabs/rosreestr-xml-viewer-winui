using System.Text;
using System.Text.RegularExpressions;

namespace RosreestrViewer.Core;

public static partial class RegistryService
{
    [GeneratedRegex(@"\d+(?:\s*:\s*\d+)+", RegexOptions.NonBacktracking)]
    private static partial Regex CadCandidates();
    [GeneratedRegex(@"\A\d{1,2}:\d{1,2}:\d{1,7}:\d+\z", RegexOptions.NonBacktracking)]
    private static partial Regex CadNumber();
    public static string[] CadastralNumbers(string text)
    {
        var numbers = new SortedSet<string>(StringComparer.Ordinal);
        // Consume the entire numeric chain before checking its shape: a fifth block
        // or an oversized block must not turn into a valid-looking substring.
        foreach (Match match in CadCandidates().Matches(text))
        {
            int end = match.Index + match.Length;
            while (end < text.Length && char.IsWhiteSpace(text[end])) end++;
            if (end < text.Length && text[end] == ':') continue;
            var number = string.Concat(match.Value.Where(c => !char.IsWhiteSpace(c)));
            if (CadNumber().IsMatch(number)) numbers.Add(number);
        }
        return numbers.ToArray();
    }
    public static string CompareCadastralLists(string first, string second)
    {
        var a = CadastralNumbers(first); var b = CadastralNumbers(second);
        var sb = new StringBuilder("<h1>Списки кадастровых номеров</h1><p>Уникальные номера; повторы и пробелы не влияют на сравнение.</p>");
        void Section(string title, IEnumerable<string> values)
        {
            var list = values.ToArray();
            sb.Append($"<h2>{title} · {list.Length}</h2>");
            sb.Append(list.Length == 0 ? "<p class='muted'>Нет номеров</p>" : "<pre>" + ReportRenderer.E(string.Join("\n", list)) + "</pre>");
        }
        Section("Первый список", a); Section("Второй список", b);
        Section("Только в первом", a.Except(b, StringComparer.Ordinal));
        Section("Только во втором", b.Except(a, StringComparer.Ordinal));
        Section("Общие", a.Intersect(b, StringComparer.Ordinal));
        return sb.ToString();
    }
    public static string Csv(IEnumerable<ExtractDocument> documents)
    {
        static string Cell(string value)
        {
            if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') || value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@')) value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        var sb = new StringBuilder("Кадастровый номер;Вид;Адрес;Площадь;Дата выписки;Правообладатели;Права;Обременения;Файл;SHA256\r\n");
        foreach (var d in documents) sb.AppendLine(string.Join(";", new[] { d.CadastralNumber, d.Kind, d.Address, d.Area, d.Date, string.Join(" | ", d.Rights.Select(r => r.Holder)), string.Join(" | ", d.Rights.Select(r => $"{r.Type} {r.Share} {r.Number}")), string.Join(" | ", d.Encumbrances), d.EntryName, d.Hash }.Select(Cell)));
        return sb.ToString();
    }
    public static string Compare(ExtractDocument left, ExtractDocument right)
    {
        var sb = new StringBuilder($"<h1>Сравнение выписок</h1><p>{ReportRenderer.E(left.EntryName)} → {ReportRenderer.E(right.EntryName)}</p>");
        if (left.CadastralNumber != right.CadastralNumber) sb.Append("<div class='note'>Выбраны разные кадастровые номера. Это сравнение разных объектов.</div>");
        sb.Append("<table><tr><th>Поле</th><th>Первая выписка</th><th>Вторая выписка</th></tr>");
        // Repeated nodes include occurrence indices, so associations are not lost through flattening.
        var a = left.Fields.GroupBy(f => f.Path).ToDictionary(g => g.Key, g => string.Join("\n", g.Select(f => f.Value).Order(StringComparer.Ordinal)));
        var b = right.Fields.GroupBy(f => f.Path).ToDictionary(g => g.Key, g => string.Join("\n", g.Select(f => f.Value).Order(StringComparer.Ordinal)));
        int changes = 0;
        foreach (var key in a.Keys.Union(b.Keys).Order(StringComparer.Ordinal))
        {
            string av = a.GetValueOrDefault(key, ""), bv = b.GetValueOrDefault(key, "");
            if (av == bv) continue;
            changes++;
            sb.Append($"<tr><td class='muted'>{ReportRenderer.E(key)}</td><td>{ReportRenderer.E(av)}</td><td>{ReportRenderer.E(bv)}</td></tr>");
        }
        return sb.Append($"</table><p>Изменённых путей: {changes}. Индексы отражают порядок повторяющихся узлов в документе. Перестановка узлов также видна как изменение; юридический смысл проверяйте по исходникам.</p>").ToString();
    }
}
