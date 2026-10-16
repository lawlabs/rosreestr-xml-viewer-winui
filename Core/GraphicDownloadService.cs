using System.Security.Cryptography;
using System.Text;

namespace RosreestrViewer.Core;

public sealed class GraphicDownloadService(string cacheFolder)
{
    public static bool Allowed(Uri uri) => uri.Scheme == "https" && uri.IsDefaultPort && new[] { "rosreestr.gov.ru", "rosreestr.ru", "roscadastre.ru", "nspd.gov.ru" }.Any(d => uri.Host.Equals(d, StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith("." + d, StringComparison.OrdinalIgnoreCase));
    public async Task<Attachment> DownloadAsync(Attachment attachment, CancellationToken token = default)
    {
        if (!Uri.TryCreate(attachment.Reference, UriKind.Absolute, out var uri) || !Allowed(uri)) throw new InvalidDataException("Загрузка доступна только по HTTPS-ссылкам из XML на официальные домены Росреестра, Роскадастра и НСПД.");
        Directory.CreateDirectory(cacheFolder);
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uri.AbsoluteUri)));
        string target = Path.Combine(cacheFolder, key + ".bin");
        if (File.Exists(target))
        {
            if (new FileInfo(target).Length > ImportService.MaxAttachmentBytes) throw new InvalidDataException("Кэшированный файл превышает 24 МБ.");
            var bytes = await File.ReadAllBytesAsync(target, token); return Resolved(attachment, bytes);
        }
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token); timeout.CancelAfter(TimeSpan.FromSeconds(25));
        var requestToken = timeout.Token;
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(25) };
        for (int hop = 0; hop < 5; hop++)
        {
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, requestToken);
            if ((int)response.StatusCode is >= 300 and < 400)
            {
                var location = response.Headers.Location ?? throw new InvalidDataException("Перенаправление без адреса.");
                uri = new Uri(uri, location);
                if (!Allowed(uri)) throw new InvalidDataException("Перенаправление за пределы официальных HTTPS-доменов отклонено.");
                continue;
            }
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > ImportService.MaxAttachmentBytes) throw new InvalidDataException("Графика превышает 24 МБ.");
            await using var stream = await response.Content.ReadAsStreamAsync(requestToken);
            using var memory = new MemoryStream();
            var buffer = new byte[81920]; int n;
            while ((n = await stream.ReadAsync(buffer, requestToken)) > 0) { if (memory.Length + n > ImportService.MaxAttachmentBytes) throw new InvalidDataException("Графика превышает 24 МБ."); await memory.WriteAsync(buffer.AsMemory(0, n), requestToken); }
            var data = memory.ToArray(); var resolved = Resolved(attachment, data);
            await File.WriteAllBytesAsync(target, data, token);
            return resolved;
        }
        throw new InvalidDataException("Слишком много перенаправлений.");
    }
    static Attachment Resolved(Attachment a, byte[] bytes)
    {
        var ext = DetectExtension(bytes);
        if (ext.Length == 0) throw new InvalidDataException("Сервер вернул неподдерживаемый файл или страницу ошибки вместо графики.");
        string name = Path.GetFileNameWithoutExtension(a.Name);
        if (name.Length == 0) name = "Графическое вложение";
        return a with { Content = bytes, Name = name + ext };
    }
    public static string DetectExtension(byte[] b) => b.AsSpan().StartsWith("%PDF-"u8) ? ".pdf"
        : b.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? ".png"
        : b.AsSpan().StartsWith(new byte[] { 255, 216, 255 }) ? ".jpg"
        : b.AsSpan().StartsWith("GIF8"u8) ? ".gif" : b.AsSpan().StartsWith("BM"u8) ? ".bmp" : "";
}
