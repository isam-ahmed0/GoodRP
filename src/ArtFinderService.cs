using System.Text.Json;

namespace GoodRP;

public static class ArtFinderService
{
    private static readonly HttpClient _httpClient = new();
    private static readonly Dictionary<string, string> _cache = new();

    public static async Task<string?> FindArtAsync(string title, string artist, string? album)
    {
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(artist))
            return null;

        var cacheKey = NormalizeKey(title, artist);
        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached;

        string? url = await SearchDeezerAsync(title, artist);
        url ??= await SearchITunesAsync(title, artist, album);
        url ??= await SearchYouTubeAsync(title, artist);

        if (url != null)
            _cache[cacheKey] = url;

        return url;
    }

    private static async Task<string?> SearchDeezerAsync(string title, string artist)
    {
        try
        {
            var cleanTitle = CleanTitle(title);
            var cleanArtist = CleanTitle(artist);
            var query = Uri.EscapeDataString($"{cleanArtist} {cleanTitle}");
            var response = await _httpClient.GetAsync($"https://api.deezer.com/search?q={query}&limit=5&order=RANKING");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(body);
            var data = doc.RootElement.GetProperty("data");
            if (data.GetArrayLength() == 0) return null;

            var best = FindBestMatch(data, cleanTitle, cleanArtist);
            if (best == null) return null;

            var el = best.Value;
            if (el.TryGetProperty("album", out var album) &&
                album.TryGetProperty("cover_big", out var cover))
            {
                return cover.GetString();
            }
        }
        catch { }

        return null;
    }

    private static async Task<string?> SearchITunesAsync(string title, string artist, string? album)
    {
        try
        {
            var cleanTitle = CleanTitle(title);
            var cleanArtist = CleanTitle(artist);
            var terms = new List<string>();
            if (!string.IsNullOrWhiteSpace(cleanArtist)) terms.Add(cleanArtist);
            if (!string.IsNullOrWhiteSpace(cleanTitle)) terms.Add(cleanTitle);
            var query = Uri.EscapeDataString(string.Join(" ", terms));

            var response = await _httpClient.GetAsync($"https://itunes.apple.com/search?term={query}&entity=song&limit=5");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(body);
            var results = doc.RootElement.GetProperty("results");
            if (results.GetArrayLength() == 0) return null;

            var best = FindBestMatch(results, cleanTitle, cleanArtist);
            if (best == null) return null;

            var el = best.Value;
            if (el.TryGetProperty("artworkUrl100", out var art100))
            {
                var url = art100.GetString();
                if (url != null)
                {
                    url = url.Replace("100x100bb", "600x600bb");
                    return url;
                }
            }
        }
        catch { }

        return null;
    }

    private static readonly string[] _invidiousInstances = new[]
    {
        "https://inv.riverside.rocks",
        "https://invidious.snopyta.org",
        "https://vid.puffyan.us",
        "https://invidious.nerdvpn.de",
        "https://inv.zzls.xyz",
        "https://inv.bp.projectsegfau.lt",
        "https://invidious.jing.rocks",
        "https://invidious.no-logs.com"
    };

    private static string CleanTitle(string title)
    {
        var t = title;
        var suffixes = new[]
        {
            " - youtube", " - youtube music", " - youtube video",
            " (official video)", " (official music video)", " (official lyric video)",
            " (music video)", " (lyric video)", " (audio)", " (hd)",
            " (4k)", " (1080p)", " (60fps)", " (lyrics)",
            " (official)", " (official audio)",
            " | official video", " | official music video",
            " | music video", " | official audio"
        };
        var lower = t.ToLowerInvariant();
        foreach (var suffix in suffixes)
        {
            if (lower.EndsWith(suffix))
            {
                t = t[..^suffix.Length];
                break;
            }
        }
        return t.Trim();
    }

    private static async Task<string?> SearchYouTubeAsync(string title, string artist)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        try
        {
            var cleanTitle = CleanTitle(title);
            var queries = new List<string>();

            if (!string.IsNullOrWhiteSpace(artist))
            {
                var cleanArtist = CleanTitle(artist);
                queries.Add(Uri.EscapeDataString($"{cleanArtist} {cleanTitle}"));
                queries.Add(Uri.EscapeDataString(cleanTitle));
            }
            else
            {
                queries.Add(Uri.EscapeDataString(cleanTitle));
            }

            string? result = null;

            foreach (var instance in _invidiousInstances)
            {
                foreach (var query in queries)
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        var response = await _httpClient.GetAsync($"{instance}/api/v1/search?q={query}&sort_by=relevance&limit=5", cts.Token);
                        if (!response.IsSuccessStatusCode) continue;

                        var body = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(body);
                        var root = doc.RootElement;

                        JsonElement? best = null;
                        var titleNorm = Normalize(cleanTitle);

                        foreach (var item in root.EnumerateArray())
                        {
                            var itemTitle = Normalize(GetStringProp(item, "title") ?? "");
                            if (titleNorm.Length > 2 && itemTitle.Contains(titleNorm))
                            {
                                best = item;
                                break;
                            }
                        }

                        best ??= root.GetArrayLength() > 0 ? root[0] : null;
                        if (best == null) continue;

                        var thumbnails = best.Value.GetProperty("videoThumbnails");
                        string? thumbUrl = null;
                        foreach (var t in thumbnails.EnumerateArray())
                        {
                            var url = t.GetProperty("url").GetString();
                            if (url != null && url.Contains("maxresdefault"))
                            {
                                thumbUrl = url;
                                break;
                            }
                            thumbUrl ??= url;
                        }

                        if (thumbUrl != null)
                        {
                            result = thumbUrl;
                            break;
                        }
                    }
                    catch { continue; }
                }
                if (result != null) break;
            }

            return result;
        }
        catch { return null; }
    }

    private static JsonElement? FindBestMatch(JsonElement items, string title, string artist)
    {
        if (items.GetArrayLength() == 0) return null;

        var titleNorm = Normalize(title);
        var artistNorm = Normalize(artist);

        foreach (var item in items.EnumerateArray())
        {
            var itemTitle = Normalize(GetStringProp(item, "title") ?? GetStringProp(item, "trackName") ?? "");
            var itemArtist = Normalize(GetDeezerArtist(item) ?? GetStringProp(item, "artistName") ?? "");

            if (titleNorm.Length > 2 && itemTitle.Contains(titleNorm))
                return item;
            if (artistNorm.Length > 2 && itemArtist.Contains(artistNorm))
                return item;
        }

        return items[0];
    }

    private static string? GetStringProp(JsonElement el, string name)
    {
        return el.TryGetProperty(name, out var prop) ? prop.GetString() : null;
    }

    private static string? GetDeezerArtist(JsonElement item)
    {
        if (item.TryGetProperty("artist", out var artist) && artist.TryGetProperty("name", out var name))
            return name.GetString();
        return null;
    }

    private static string Normalize(string s)
    {
        return string.IsNullOrWhiteSpace(s) ? "" : s.ToLowerInvariant().Trim();
    }

    private static string NormalizeKey(string title, string artist)
    {
        return $"{Normalize(artist)}|{Normalize(title)}";
    }

    public static void ClearCache()
    {
        _cache.Clear();
    }

    public static void TrimCache(int maxEntries = 100)
    {
        if (_cache.Count <= maxEntries) return;

        var keysToRemove = _cache.Keys.Take(_cache.Count - maxEntries).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }
    }
}
