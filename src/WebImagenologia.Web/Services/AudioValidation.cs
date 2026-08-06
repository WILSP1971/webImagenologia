namespace WebImagenologia.Web.Services;

public static class AudioValidation
{
    public static readonly string[] AllowedContentTypes =
    [
        "audio/mpeg",
        "audio/wav",
        "audio/ogg",
        "audio/mp4",
        "audio/x-m4a",
        "audio/aac",
        "audio/flac",
        "audio/webm",
        "audio/x-wav",
        "audio/x-aiff"
    ];

    private static readonly string[] AllowedExtensions =
    [
        ".mp3", ".wav", ".ogg", ".m4a", ".aac", ".flac", ".webm", ".wma",
        ".opus", ".amr", ".3gp", ".aiff", ".aif", ".mp4", ".mpeg", ".mpga", ".weba"
    ];

    private static readonly string[] BlockedExtensions =
    [
        ".exe", ".bat", ".cmd", ".msi", ".dll", ".com", ".scr"
    ];

    public const long MaxSizeBytes = 25 * 1024 * 1024;

    public static bool IsAllowedContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsAllowedFile(string? fileName, string? contentType)
    {
        if (IsBlockedExtension(fileName))
        {
            return false;
        }

        if (IsAllowedContentType(contentType))
        {
            return true;
        }

        return HasAllowedAudioExtension(fileName);
    }

    public static bool IsAllowedSize(long sizeBytes) => sizeBytes > 0 && sizeBytes <= MaxSizeBytes;

    private static bool IsBlockedExtension(string? fileName)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty);
        return BlockedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private static bool HasAllowedAudioExtension(string? fileName)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty);
        return !string.IsNullOrWhiteSpace(extension)
            && AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}
