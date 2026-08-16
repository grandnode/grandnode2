namespace Grand.SharedKernel.Extensions;

public static class FileExtensions
{
    /// <summary>
    ///     Hard upper bound (in KB) for a single file-upload attribute (contact/checkout/product attribute uploads),
    ///     enforced regardless of the per-attribute ValidationFileMaximumSize configuration. Must be checked against
    ///     IFormFile.Length before the request body is buffered into memory.
    /// </summary>
    public const int MaxAttributeUploadFileSizeKb = 10 * 1024; // 10 MB

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