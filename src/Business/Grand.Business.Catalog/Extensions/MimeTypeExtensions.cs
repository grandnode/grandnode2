using Microsoft.AspNetCore.StaticFiles;

namespace Grand.Business.Catalog.Extensions;

public static class MimeTypeExtensions
{
    public static string GetMimeTypeFromFilePath(string filePath)
    {
        new FileExtensionContentTypeProvider().TryGetContentType(filePath, out var mimeType);
        //set to jpeg in case mime type cannot be found
        return mimeType ?? "image/jpeg";
    }
}