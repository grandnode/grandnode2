namespace Grand.SharedKernel.Extensions;

public static class FileExtensions
{
    public static IList<string> GetAllowedMediaFileTypes(string allowedFileTypes)
    {
        if (string.IsNullOrEmpty(allowedFileTypes))
            return new List<string> { ".gif", ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
        //a trailing or doubled comma leaves an empty entry, which would allow a file without an extension
        return allowedFileTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant()).ToList();
    }
    public static bool IsAllowedMediaFileType(this IEnumerable<string> allowedFileTypes, string fileExtension)
    {
        return !string.IsNullOrEmpty(fileExtension) &&
               allowedFileTypes.Any(ft => ft.Equals(fileExtension, StringComparison.OrdinalIgnoreCase));
    }
}