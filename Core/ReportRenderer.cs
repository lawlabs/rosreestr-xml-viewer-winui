using System.Net;
using System.Text;

namespace RosreestrViewer.Core;

public static class ReportRenderer
{
    public static string E(string? s) => WebUtility.HtmlEncode(s ?? "");
    public static string Page(string body, bool dark = false, string title = "Выписки ЕГРН") => $$$"""
        <!doctype html><html lang="ru"><head><meta charset="utf-8"><meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data: 'self'; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'"><title>{{{E(title)}}}</title>
        <style>
        :root{color-scheme:{{{(dark ? "dark" : "light")}}};--bg:{{{(dark ? "#202020" : "#fff")}}};--fg:{{{(dark ? "#efefef" : "#20252b")}}};--muted:{{{(dark ? "#b0b8c2" : "#677482")}}};--line:{{{(dark ? "#3b4248" : "#e5e9ed")}}};--soft:{{{(dark ? "#28323a" : "#f1f6f8")}}}}
        *{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--fg);font:14px/1.6 'Segoe UI',sans-serif}main{max-width:1080px;margin:auto;padding:32px 40px 60px}h1{font-size:30px;font-weight:600;line-height:1.25;margin:8px 0 12px}h2{font-size:19px;font-weight:600;margin:30px 0 12px}p{margin:8px 0}.eyebrow{letter-spacing:1.8px;font-size:11px;color:var(--muted);text-transform:uppercase}.muted{color:var(--muted);font-size:12px}.note{background:var(--soft);border-left:3px solid #2181a5;padding:12px 16px;border-radius:4px;margin:18px 0}.stats{display:flex;gap:36px;flex-wrap:wrap;margin:22px 0 30px}.stats b{display:block;font-size:23px;font-weight:600}.stats span{font-size:12px;color:var(--muted)}table{border-collapse:collapse;width:100%;margin:12px 0}td,th{padding:10px 12px;border-bottom:1px solid var(--line);text-align:left;vertical-align:top;overflow-wrap:anywhere}th{background:var(--soft);font-size:12px;color:var(--muted);font-weight:600}td.key{width:30%;color:var(--muted)}pre{white-space:pre-wrap;overflow-wrap:anywhere;font:12px/1.6 Consolas,monospace}.hash{font:11px/1.5 Consolas,monospace;overflow-wrap:anywhere}.empty{padding:48px 0;max-width:600px}svg{display:block;width:100%;max-height:min(460px,58vh);background:var(--soft);border-radius:8px}img.plan{max-width:100%;height:auto}article{break-after:page}article:last-child{break-after:auto}tr{break-inside:avoid}@media print{body{--bg:white;--fg:black;--muted:#555;--line:#ddd;--soft:#f3f5f6}main{padding:0}h1{font-size:24px}h1,h2{break-after:avoid}h2+table,h2+svg{break-before:avoid}svg,img.plan,.note{break-inside:avoid}svg{max-height:340px}.note{font-size:11px}thead{display:table-header-group}}@media(max-width:600px){main{padding:20px}.stats{gap:20px}h1{font-size:24px}}@media(forced-colors:active){svg path,svg circle{stroke:CanvasText}svg text{fill:CanvasText}}
        </style></head><body><main>{{{body}}}</main></body></html>
        """;
    static string Row(string key, string value) => $"<tr><td class='key'>{E(key)}</td><td>{E(value.Length == 0 ? "Не указано" : value)}</td></tr>";
    public static string Card(ExtractDocument d, bool includeGeometry = true)
    {
        if (d.IsPdfDocument) return $"<article><h1>{E(d.EntryName)}</h1><p>Самостоятельный PDF-документ. Полный оригинал доступен во вложениях и не преобразуется в XML.</p><p class='hash'>SHA-256: {d.Hash}</p></article>";
        if (d.IsNotice) return $"<article><div class='eyebrow'>Уведомление из XML</div><h1>{E(d.DisplayNumber)}</h1><div class='note'>{E(d.NoticeReason)}</div><p>{E(d.SignatureStatus)}</p><p class='muted'>Дата: {E(d.Date)} · файл: {E(d.EntryName)}</p><p class='hash'>SHA-256: {d.Hash}</p><p>Полный текст и содержание запроса смотрите во вкладках «Поля XML» и «Исходник».</p></article>";
        var sb = new StringBuilder($"<article><div class='eyebrow'>Локальное представление XML · {E(d.Kind)}</div><h1>{E(d.DisplayNumber)}</h1><p>{E(d.Address)}</p><div class='stats'><div><b>{E(d.Area.Length == 0 ? "—" : d.Area)}</b><span>Площадь из XML</span></div><div><b>{d.Rights.Count}</b><span>Записей о правах</span></div><div><b>{d.Rings.Count}</b><span>Контуров</span></div></div>");
        sb.Append("<table>" + Row("Вид объекта", d.Kind) + Row("Назначение / использование", d.Purpose) + Row("Дата выписки", d.Date) + Row("Кадастровая стоимость", d.Cost) + Row("Статус", d.Status) + Row("Файл", d.EntryName) + "</table>");
        sb.Append("<h2>Права и правообладатели</h2>");
        if (d.Rights.Count == 0) sb.Append("<p class='muted'>Записи не найдены в распознаваемых разделах. Это не подтверждает отсутствие зарегистрированных прав; проверьте поля и исходник.</p>");
        else
        {
            sb.Append("<table><tr><th>Правообладатель</th><th>Право / доля</th><th>Номер и дата</th></tr>");
            foreach (var r in d.Rights) sb.Append($"<tr><td>{E(r.Holder)}</td><td>{E(r.Type)}<br>{E(r.Share)}</td><td>{E(r.Number)}<br>{E(r.Date)}</td></tr>");
            sb.Append("</table>");
        }
        sb.Append("<h2>Ограничения и обременения</h2>");
        if (d.Encumbrances.Count == 0) sb.Append("<p class='muted'>В известных разделах записи не найдены. Для проверки используйте исходный документ.</p>");
        else foreach (var e in d.Encumbrances) sb.Append($"<p>{E(e)}</p>");
        sb.Append($"<h2>Источник и проверки</h2><p>{E(d.SignatureStatus)}</p><p class='muted'>SHA-256 фиксирует содержимое исходника и помогает обнаружить изменения. Криптографическая проверка электронной подписи не выполнена.</p><p class='hash'>{d.Hash}</p><table>" + Row("Корень XML", d.RootName) + Row("Пространство имён", d.Namespace) + Row("Версия", d.SchemaVersion) + "</table>");
        foreach (var w in d.Warnings) sb.Append($"<div class='note'>{E(w)}</div>");
        if (includeGeometry && d.Rings.Count > 0) sb.Append("<h2>Схема по координатам</h2>" + GeometryRenderer.Html(d));
        foreach (var a in d.Attachments.Where(a => a.IsImage && a.Content != null)) sb.Append($"<h2>{E(a.Name)}</h2><img class='plan' alt='Графическое вложение XML' src='data:{Mime(a.Extension)};base64,{Convert.ToBase64String(a.Content!)}'>");
        return sb.Append("</article>").ToString();
    }
    public static string Mime(string ext) => ext switch { ".png" => "image/png", ".gif" => "image/gif", ".bmp" => "image/bmp", _ => "image/jpeg" };
    public static string Fields(ExtractDocument doc, string filter = "")
    {
        var fields = doc.Fields.Where(f => filter.Length == 0 || (f.Path + " " + f.Value).Contains(filter, StringComparison.OrdinalIgnoreCase)).ToArray();
        var sb = new StringBuilder($"<h1>Поля XML</h1><p class='muted'>{fields.Length} полей · пути без префиксов пространств имён; индексы различают повторяющиеся узлы</p><table><tr><th>Путь</th><th>Значение</th></tr>");
        foreach (var f in fields) sb.Append($"<tr><td class='muted'>{E(f.Path)}</td><td>{E(f.Value)}</td></tr>");
        return sb.Append("</table>").ToString();
    }
    public static string Welcome() => "<div class='empty'><div class='eyebrow'>Рабочее пространство</div><h1>Выписки под рукой</h1><p>Откройте XML, ZIP с выписками или папку документов. Адрес, права, вложения и координаты будут собраны в одном месте.</p><h2>Сначала исходный документ</h2><p class='muted'>Приложение читает локальные файлы. Готовый PDF и подпись сохраняются как вложения. Для получения графики из интернета используйте отдельную команду.</p><h2>Попробуйте на примере</h2><p class='muted'>Демонстрационный набор содержит вымышленные участок, помещение и две версии выписки для сравнения.</p></div>";
}
