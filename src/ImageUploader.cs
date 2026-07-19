using System.Net.Http.Headers;
using System.Text.Json;
using Windows.Storage.Streams;

namespace GoodRP;

public static class ImageUploader
{
    private static readonly HttpClient _httpClient = new();
    private static readonly Dictionary<string, string> _cache = new();

    public static async Task<string?> UploadThumbnailAsync(IRandomAccessStreamReference? thumbnail, string cacheKey)
    {
        if (thumbnail == null) return null;

        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            using var stream = await thumbnail.OpenReadAsync();
            using var memStream = new MemoryStream();
            await stream.AsStreamForRead().CopyToAsync(memStream);
            var imageBytes = memStream.ToArray();

            if (imageBytes.Length == 0) return null;

            return await UploadToProvidersAsync(imageBytes, cacheKey);
        }
        catch
        {
            return null;
        }
    }

    public static async Task<string?> UploadArtworkUrlAsync(string? artworkUrl, string cacheKey)
    {
        if (string.IsNullOrEmpty(artworkUrl)) return null;

        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            using var response = await _httpClient.GetAsync(artworkUrl);
            if (!response.IsSuccessStatusCode) return null;

            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            if (imageBytes.Length == 0) return null;

            return await UploadToProvidersAsync(imageBytes, cacheKey);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> UploadToProvidersAsync(byte[] imageData, string cacheKey)
    {
        var providers = ConfigManager.Config.ImageProviders;
        if (providers == null || providers.Count == 0) return null;

        foreach (var provider in providers)
        {
            string? url = provider.ToLowerInvariant() switch
            {
                "telegraph" => await UploadToTelegraphAsync(imageData),
                "cloudinary" => await UploadToCloudinaryAsync(imageData),
                "postimage" => await UploadToPostImageAsync(imageData),
                _ => null
            };

            if (url != null)
            {
                _cache[cacheKey] = url;
                return url;
            }
        }

        return null;
    }

    private static async Task<string?> UploadToTelegraphAsync(byte[] imageData)
    {
        try
        {
            var (mime, ext) = DetectFormat(imageData);
            var fileContent = new ByteArrayContent(imageData);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(mime);

            using var content = new MultipartFormDataContent
            {
                { fileContent, "file", $"img.{ext}" }
            };

            var response = await _httpClient.PostAsync("https://telegra.ph/upload", content);
            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.GetArrayLength() > 0)
                {
                    var src = doc.RootElement[0].GetProperty("src").GetString();
                    if (src != null)
                        return $"https://telegra.ph{src}";
                }
            }
        }
        catch { }

        return null;
    }

    private static async Task<string?> UploadToCloudinaryAsync(byte[] imageData)
    {
        var cloudName = ConfigManager.Config.CloudinaryCloudName;
        var uploadPreset = ConfigManager.Config.CloudinaryUploadPreset;
        if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(uploadPreset))
            return null;

        try
        {
            var (mime, ext) = DetectFormat(imageData);
            var fileContent = new ByteArrayContent(imageData);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(mime);

            using var content = new MultipartFormDataContent
            {
                { fileContent, "file", $"album_art.{ext}" },
                { new StringContent(uploadPreset), "upload_preset" }
            };

            var response = await _httpClient.PostAsync($"https://api.cloudinary.com/v1_1/{cloudName}/image/upload", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                var url = doc.RootElement.GetProperty("secure_url").GetString();
                return url;
            }
        }
        catch { }

        return null;
    }

    private static (string mime, string ext) DetectFormat(byte[] data)
    {
        if (data.Length < 4) return ("image/png", "png");
        if (data[0] == 0xFF && data[1] == 0xD8) return ("image/jpeg", "jpg");
        if (data[0] == 0x89 && data[1] == 0x50) return ("image/png", "png");
        if (data[0] == 0x47 && data[1] == 0x49) return ("image/gif", "gif");
        if (data[0] == 0x42 && data[1] == 0x4D) return ("image/bmp", "bmp");
        if (data[0] == 0x52 && data[1] == 0x49) return ("image/webp", "webp");
        return ("image/png", "png");
    }

    private static async Task<string?> UploadToPostImageAsync(byte[] imageData)
    {
        var apiKey = ConfigManager.Config.PostImageApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        try
        {
            using var content = new MultipartFormDataContent
            {
                { new ByteArrayContent(imageData), "file", "album_art.png" },
                { new StringContent(apiKey), "key" }
            };

            var response = await _httpClient.PostAsync("https://postimg.cc/api/1/upload", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("url", out var urlProp))
                    return urlProp.GetString();
                if (doc.RootElement.TryGetProperty("image", out var imgProp) && imgProp.TryGetProperty("url", out var imgUrl))
                    return imgUrl.GetString();
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
