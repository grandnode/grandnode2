namespace Grand.SharedKernel.Extensions;

public static class FileExtensions
{
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