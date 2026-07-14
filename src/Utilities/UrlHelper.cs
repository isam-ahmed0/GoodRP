namespace GoodRP.Utilities;

public static class UrlHelper
{
    public static bool IsDiscordCdnUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        return url.Contains("discordapp.com") || url.Contains("discordapp.net");
    }
}
