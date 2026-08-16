namespace Grand.SharedKernel.Extensions;

public static class FileExtensions
{
    /// <summary>
    ///     Hard upper bound (in bytes) for a single file-upload attribute request (contact/checkout/product attribute
    ///     uploads), applied via [RequestSizeLimit] so ASP.NET Core rejects an oversized request before the body is
    ///     ever buffered into memory - independent of the per-attribute ValidationFileMaximumSize configuration.
    /// </summary>
    public const long MaxAttributeUploadRequestBytes = 10 * 1024 * 1024; // 10 MB

    public static IList<string> GetAllowedMediaFileTypes(string allowedFileTypes)
    {
        if (string.IsNullOrEmpty(allowedFileTypes))
            return new List<string> { ".gif", ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
        return allowedFileTypes.Split(',').Select(x => x.Trim().ToLowerInvariant()).ToList();
    }
    public static bool IsAllowedMediaFileType(this IEnumerable<string> allowedFileTypes, string fileExtension)
    {
        return allowedFileTypes.Any(ft => ft.Equals(fileExtension, StringComparison.OrdinalIgnoreCase));
    }
}