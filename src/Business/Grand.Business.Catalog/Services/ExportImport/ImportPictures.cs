using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Media;

namespace Grand.Business.Catalog.Services.ExportImport;

/// <summary>
///     Pictures of the catalogue row imports. A row downloads its pictures before it writes anything, so a cancelled
///     token leaves the row untouched instead of half saved; storing them never takes the token.
/// </summary>
internal static class ImportPictures
{
    /// <summary>
    ///     Only http(s) URLs to public or explicitly allowed hosts are downloaded - a local path would let the import read
    ///     any file on the server
    /// </summary>
    /// <returns>The image, or null when there is no URL or it could not be downloaded</returns>
    public static Task<DownloadedImage> Download(string url, string[] allowedPrivateHosts,
        CancellationToken cancellationToken)
    {
        return string.IsNullOrEmpty(url)
            ? Task.FromResult<DownloadedImage>(null)
            : DownloadUrl.DownloadImage(url, allowedPrivateHosts, cancellationToken);
    }

    /// <summary>Whether the stored picture has the image's content, as sent or after validation (like in database)</summary>
    public static async Task<bool> IsStored(IPictureService pictureService, string pictureId, DownloadedImage image)
    {
        if (string.IsNullOrEmpty(pictureId))
            return false;

        var existingPicture = await pictureService.GetPictureById(pictureId);
        if (existingPicture == null)
            return false;

        var existingBinary = await pictureService.LoadPictureBinary(existingPicture);
        return existingBinary.SequenceEqual(image.Binary) ||
               existingBinary.SequenceEqual(pictureService.ValidatePicture(image.Binary, image.MimeType));
    }

    /// <summary>Stores the image as the entity's picture</summary>
    /// <returns>The new picture, or null when there is no image or the current picture already has its content</returns>
    public static async Task<Picture> Save(IPictureService pictureService, DownloadedImage image, string name,
        string currentPictureId)
    {
        if (image == null || await IsStored(pictureService, currentPictureId, image))
            return null;

        return await pictureService.InsertPicture(image.Binary, image.MimeType, pictureService.GetPictureSeName(name));
    }
}
