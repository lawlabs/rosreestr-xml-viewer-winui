using System.Globalization;
using System.Net;
using System.Text;

namespace RosreestrViewer.Core;

public static class GeometryRenderer
{
    static string F(double n) => n.ToString("0.###", CultureInfo.InvariantCulture);
    public static bool Closed(BoundaryRing ring) => ring.Points.Count >= 4 && ring.Points[0].X == ring.Points[^1].X && ring.Points[0].Y == ring.Points[^1].Y;
    public static double Area(BoundaryRing ring)
    {
        if (!Closed(ring)) return 0;
        // Translate to origin to avoid cancellation for large cadastral coordinates.
        double x0 = ring.Points[0].X, y0 = ring.Points[0].Y, sum = 0;
        for (int i = 1; i < ring.Points.Count; i++) sum += (ring.Points[i - 1].X - x0) * (ring.Points[i].Y - y0) - (ring.Points[i].X - x0) * (ring.Points[i - 1].Y - y0);
        return Math.Abs(sum) / 2;
    }
    public static double Length(BoundaryRing ring) => ring.Points.Zip(ring.Points.Skip(1), (a, b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2))).Sum();
    public static ILookup<string, BoundaryRing> Systems(IReadOnlyList<BoundaryRing> rings) =>
        rings.Select((r, i) => (Ring: r, Key: string.IsNullOrEmpty(r.CoordinateSystem) ? "Не указана / контур " + (i + 1) : r.CoordinateSystem))
            .ToLookup(x => x.Key, x => x.Ring);
    public static string Svg(IReadOnlyList<BoundaryRing> rings)
    {
        if (rings.Count == 0) return "";
        var points = rings.SelectMany(r => r.Points).ToArray();
        double minX = points.Min(p => p.X), maxX = points.Max(p => p.X), minY = points.Min(p => p.Y), maxY = points.Max(p => p.Y);
        double scale = Math.Min(780 / Math.Max(maxY - minY, 1), 470 / Math.Max(maxX - minX, 1));
        double width = (maxY - minY) * scale, height = (maxX - minX) * scale;
        double ox = (900 - width) / 2, oy = (590 - height) / 2;
        double PX(BoundaryPoint p) => ox + (p.Y - minY) * scale;
        double PY(BoundaryPoint p) => oy + (maxX - p.X) * scale;
        var sb = new StringBuilder("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 900 590' role='img' aria-label='Схема контуров по координатам XML'>");
        sb.Append("<defs><pattern id='grid' width='30' height='30' patternUnits='userSpaceOnUse'><path d='M 30 0 L 0 0 0 30' fill='none' stroke='#cbd5e1' stroke-opacity='.35'/></pattern></defs><rect width='900' height='590' fill='url(#grid)'/>");
        foreach (var ring in rings)
        {
            string path = "M " + string.Join(" L ", ring.Points.Select(p => $"{F(PX(p))} {F(PY(p))}"));
            sb.Append($"<path d='{path}' fill='{(Closed(ring) ? "#2c7d9d" : "none")}' fill-opacity='.14' stroke='#2181a5' stroke-width='2.5' stroke-linejoin='round'/>");
            foreach (var p in ring.Points.DistinctBy(p => (p.X, p.Y)).Take(500))
                sb.Append($"<circle cx='{F(PX(p))}' cy='{F(PY(p))}' r='3.5' fill='#2181a5'/><text x='{F(PX(p) + 8)}' y='{F(PY(p) - 8)}' font-family='Segoe UI,sans-serif' font-size='13' fill='currentColor'>{WebUtility.HtmlEncode(p.Number)}</text>");
        }
        sb.Append("<path d='M855 72 L855 28 M850 36 L855 28 L860 36' stroke='currentColor' stroke-width='2' fill='none'/><text x='849' y='20' fill='currentColor' font-family='Segoe UI' font-size='13'>X</text><text x='815' y='93' fill='currentColor' font-family='Segoe UI' font-size='12'>Y →</text>");
        double bar = Math.Pow(10, Math.Floor(Math.Log10(150 / scale)));
        sb.Append($"<path d='M40 550 H{F(40 + bar * scale)} M40 545 V555 M{F(40 + bar * scale)} 545 V555' stroke='currentColor' fill='none' stroke-width='2'/><text x='40' y='575' fill='currentColor' font-family='Segoe UI' font-size='12'>{F(bar)} ед. координат</text></svg>");
        return sb.ToString();
    }
    public static string Html(ExtractDocument doc)
    {
        if (doc.Rings.Count == 0) return "<div class='empty'><h2>Нет координат для построения</h2><p>Откройте графическое вложение или загрузите изображение по ссылке из XML. Если этих данных нет, закажите выписку с графической частью. По адресу или площади восстановить границы нельзя.</p></div>";
        var sb = new StringBuilder("<div class='note'>Схема построена приложением по координатам XML. Официальная графическая часть выписки не заменяется. X показан вверх, Y вправо; для МСК это север и восток. Географические координаты требуют отдельной проекции.</div>");
        // Never overlay different systems or unidentified independent rings.
        foreach (var group in Systems(doc.Rings))
        {
            sb.Append($"<h2>Система координат: {WebUtility.HtmlEncode(group.Key)}</h2>");
            sb.Append(Svg(group.ToArray()));
            sb.Append("<table><tr><th>Контур</th><th>Точек</th><th>Площадь в ед.²</th><th>Длина в ед.</th></tr>");
            foreach (var r in group)
            {
                sb.Append($"<tr><td>{WebUtility.HtmlEncode(r.Name)}</td><td>{r.Points.Count}</td><td>{(Closed(r) ? F(Area(r)) : "Не замкнут")}</td><td>{F(Length(r))}</td></tr>");
            }
            sb.Append("</table><p class='muted'>Расчёты по каждому контуру отдельно. Отверстия, топология и площадь объединения не определяются. Единицы — исходные единицы системы координат.</p>");
        }
        sb.Append("<h2>Каталог координат</h2><table><tr><th>Контур / точка</th><th>X</th><th>Y</th><th>Погрешность</th></tr>");
        foreach (var r in doc.Rings) foreach (var p in r.Points) sb.Append($"<tr><td>{WebUtility.HtmlEncode(r.Name + " / " + p.Number)}</td><td>{F(p.X)}</td><td>{F(p.Y)}</td><td>{WebUtility.HtmlEncode(p.Accuracy)}</td></tr>");
        return sb.Append("</table>").ToString();
    }
}
