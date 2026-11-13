using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.SharedKernel.Extensions;
using Grand.Web.Common.Extensions;
using Grand.Web.Models.Orders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace Grand.Web.Extensions;

/// <summary>
///     The images a customer adds to a merchandise return: photos of the items with the return, and an image with a
///     message. Checked as the other storefront uploads are: the extension against the store's image types and the size
///     before the file is read
/// </summary>
public static class MerchandiseReturnUploads
{
    public const int MaxFileBytes = 4 * 1024 * 1024;
    public const int MaxPhotosPerItem = 3;
    public const int MaxPhotosPerSubmit = 10;
    public const int MaxNoteLength = 2000;

    /// <summary>
    ///     The most a return with all its photos takes to post, the rest of the form included
    /// </summary>
    public const long MaxSubmitRequestBytes = MaxPhotosPerSubmit * (long)MaxFileBytes + 1024 * 1024;

    public const string PhotosInvalid = "MerchandiseReturns.Photos.Invalid";
    public const string PhotosRequired = "MerchandiseReturns.Photos.Required";

    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    /// <summary>
    ///     Whether the file is an image of the store's types, not empty and not too large
    /// </summary>
    /// <param name="file">The posted file</param>
    /// <param name="allowedTypes">The extensions allowed, e.g. of MediaSettings.AllowedFileTypes</param>
    public static bool IsAllowed(IFormFile file, IList<string> allowedTypes)
    {
        return file is { Length: > 0 and <= MaxFileBytes } &&
               allowedTypes.IsAllowedMediaFileType(Path.GetExtension(Path.GetFileName(file.FileName)));
    }

    /// <summary>
    ///     Why the photos posted with a return are refused, as a resource key; null when they are fine
    /// </summary>
    /// <param name="items">The items of the form</param>
    /// <param name="photos">The photos posted, by order item id</param>
    /// <param name="reasons">The store's reasons, which say whether they need a photo</param>
    /// <param name="allowedTypes">The extensions allowed</param>
    public static string PhotosError(IEnumerable<MerchandiseReturnModel.OrderItemModel> items,
        IDictionary<string, IList<IFormFile>> photos, IList<MerchandiseReturnReason> reasons,
        IList<string> allowedTypes)
    {
        var returned = items.Where(x => x.Quantity > 0).ToList();
        photos ??= new Dictionary<string, IList<IFormFile>>();

        //a photo of an item not returned belongs to no return
        if (photos.Any(x => x.Value.Count > 0 && returned.All(item => item.Id != x.Key)) ||
            photos.Values.Any(x => x.Count > MaxPhotosPerItem) ||
            photos.Values.Sum(x => x.Count) > MaxPhotosPerSubmit ||
            photos.Values.SelectMany(x => x).Any(file => !IsAllowed(file, allowedTypes)))
            return PhotosInvalid;

        var photoReasons = reasons.Where(x => x.PhotoRequired).Select(x => x.Id).ToHashSet();
        return returned.Any(item => photoReasons.Contains(item.MerchandiseReturnReasonId ?? "") &&
                                    (!photos.TryGetValue(item.Id, out var itemPhotos) || itemPhotos.Count == 0))
            ? PhotosRequired
            : null;
    }

    /// <summary>
    ///     The download of an allowed file, with the content type of its extension: what the browser says the file is
    ///     is never stored
    /// </summary>
    /// <param name="file">The posted file</param>
    /// <param name="customerId">The customer who posted it</param>
    /// <param name="referenceId">The merchandise return it belongs to</param>
    public static Download ToDownload(IFormFile file, string customerId, string referenceId)
    {
        var fileName = Path.GetFileName(file.FileName);
        return new Download {
            DownloadGuid = Guid.NewGuid(),
            CustomerId = customerId,
            UseDownloadUrl = false,
            DownloadUrl = "",
            DownloadBinary = file.GetDownloadBits(),
            ContentType = ContentTypes.TryGetContentType(fileName, out var contentType)
                ? contentType
                : "application/octet-stream",
            Filename = Path.GetFileNameWithoutExtension(fileName),
            Extension = Path.GetExtension(fileName),
            DownloadType = DownloadType.MerchandiseReturn,
            ReferenceId = referenceId
        };
    }
}
