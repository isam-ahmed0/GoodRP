using System.Net.Http.Headers;
using System.Text;
using Windows.Storage.Streams;

namespace GoodRP;

public static class ImageUploader
{
    private static readonly HttpClient _httpClient = new();
    private static readonly Dictionary<string, string> _cache = new();
    private static string _lastKey = "";

    public static async Task<string?> UploadThumbnailAsync(IRandomAccessStreamReference? thumbnail, string cacheKey)
    {
        if (thumbnail == null) return null;
        if (string.IsNullOrWhiteSpace(ConfigManager.Config.ImgurClientId)) return null;

        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            using var stream = await thumbnail.OpenReadAsync();
            using var memStream = new MemoryStream();
            await stream.AsStreamForRead().CopyToAsync(memStream);
            var imageBytes = memStream.ToArray();

            if (imageBytes.Length == 0) return null;

            return await UploadToImgurAsync(imageBytes, cacheKey);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> UploadToImgurAsync(byte[] imageData, string cacheKey)
    {
        try
        {
            var clientId = ConfigManager.Config.ImgurClientId;
            var base64 = Convert.ToBase64String(imageData);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.imgur.com/3/image");
            request.Headers.Authorization = new AuthenticationHeaderValue("Client-ID", clientId);

            var content = new MultipartFormDataContent
            {
                { new StringContent(base64), "image" },
                { new StringContent("base64"), "type" }
            };
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var linkStart = responseBody.IndexOf("\"link\":\"") + 8;
                var linkEnd = responseBody.IndexOf("\"", linkStart);
                if (linkStart > 8 && linkEnd > linkStart)
                {
                    var url = responseBody[linkStart..linkEnd];
                    _cache[cacheKey] = url;
                    _lastKey = cacheKey;
                    return url;
                }
            }
        }
        catch { }

        return null;
    }

    public static void ClearCache()
    {
        _cache.Clear();
    }

    public static void TrimCache(int maxEntries = 50)
    {
        if (_cache.Count <= maxEntries) return;

        var keysToRemove = _cache.Keys.Take(_cache.Count - maxEntries).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }
    }
}
