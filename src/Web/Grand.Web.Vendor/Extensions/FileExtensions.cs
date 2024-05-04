namespace Grand.Web.Vendor.Extensions;

public static class FileExtensions
{
    public static IList<string> GetAllowedMediaFileTypes(string allowedFileTypes)
    {
        return string.IsNullOrEmpty(allowedFileTypes)
            ? [".gif", ".jpg", ".jpeg", ".png", ".bmp", ".webp"]
            : allowedFileTypes.Split(',').Select(x => x.Trim().ToLowerInvariant()).ToList();
    }
}