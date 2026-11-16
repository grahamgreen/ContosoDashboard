namespace ContosoDashboard.Services;

public static class DocumentValidation
{
    public const long MaxFileSizeBytes = 25L * 1024 * 1024; // 26,214,400

    private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".txt"] = "text/plain",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
    };

    private static readonly HashSet<string> PreviewableTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf", "image/jpeg", "image/png"
    };

    public static IReadOnlyCollection<string> AllowedExtensions => MimeTypes.Keys;

    public static string AcceptAttribute => string.Join(",", MimeTypes.Keys);

    public const string SupportedTypesDescription =
        "PDF, Word (.doc/.docx), Excel (.xls/.xlsx), PowerPoint (.ppt/.pptx), text (.txt), JPEG, PNG";

    public static bool IsAllowedExtension(string? extension) =>
        !string.IsNullOrEmpty(extension) && MimeTypes.ContainsKey(extension);

    // Server-assigned MIME type; the browser-supplied content type is not trusted.
    public static string GetMimeType(string extension) => MimeTypes[extension];

    public static bool IsPreviewable(string mimeType) => PreviewableTypes.Contains(mimeType);

    public static string? NormalizeTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags)) return null;
        var normalized = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .ToList();
        var joined = string.Join(", ", normalized);
        return joined.Length == 0 ? null : joined.Length > 500 ? joined[..500] : joined;
    }

    public static string FormatSize(long bytes) => bytes switch
    {
        >= 1024 * 1024 => $"{bytes / 1024d / 1024d:0.0} MB",
        >= 1024 => $"{bytes / 1024d:0.0} KB",
        _ => $"{bytes} B"
    };
}
