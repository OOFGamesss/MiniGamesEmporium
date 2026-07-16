using System;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;

/// <summary>Validates user-supplied venue names and image URLs for the webview, mirroring the API's server-side rules.</summary>

namespace MiniGamesEmporium.Utility;

public static class VenueValidator
{
    public const int MaxNameLength = 40;
    public const int MaxImageDimension = 2048;

    private static readonly Regex UrlLike = new(
        @"https?://|www\.|\b[\w-]+\.(com|net|org|io|gg|co|uk|tv|me|xyz|info|app|dev|link|gov|edu)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ImageUrl = new(
        @"^https?://[^\s]+\.(jpe?g|png|webp)(\?[^\s]*)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static (bool Ok, string Error) ValidateName(string? name)
    {
        var v = (name ?? string.Empty).Trim();
        if (v.Length == 0) return (true, string.Empty);
        if (v.Length > MaxNameLength) return (false, $"Venue name must be {MaxNameLength} characters or fewer.");
        if (v.IndexOf('<') >= 0 || v.IndexOf('>') >= 0) return (false, "Venue name cannot contain HTML.");
        if (UrlLike.IsMatch(v)) return (false, "Venue name cannot contain a website link.");
        return (true, string.Empty);
    }

    public static (bool Ok, string Error) ValidateImageUrl(string? url)
    {
        var v = (url ?? string.Empty).Trim();
        if (v.Length == 0) return (true, string.Empty);
        if (!Uri.TryCreate(v, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return (false, "Venue image must be a direct http(s) image link.");
        if (uri.Host.Contains("imgur", StringComparison.OrdinalIgnoreCase))
            return (false, "Imgur links cannot be used as the UK cannot view Imgur images. Please host the image elsewhere.");
        if (!ImageUrl.IsMatch(v))
            return (false, "Venue image must be a .jpg, .png or .webp link.");
        return (true, string.Empty);
    }

    public static bool TryReadSize(byte[] data, out int width, out int height)
    {
        width = 0;
        height = 0;
        try
        {
            var info = Image.Identify(data);
            width = info.Width;
            height = info.Height;
            return width > 0 && height > 0;
        }
        catch { return false; }
    }
}
