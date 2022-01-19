namespace Grand.Web.Admin.Extensions
{
    public static class FileExtensions
    {
        public static IList<string> GetAllowedMediaFileTypes(string allowedFileTypes)
        {
            if (string.IsNullOrEmpty(allowedFileTypes))
                return new List<string> { ".gif", ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
            else
                return allowedFileTypes.Split(',').Select(x => x.Trim().ToLowerInvariant()).ToList();
        }
    }
}
