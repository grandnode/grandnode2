using Grand.Data;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.SharedKernel.Extensions;

namespace Grand.Module.Installer.Extensions;

public static class PictureExtensions
{
    public static async Task<Picture> InsertPicture(this IRepository<Picture> repository, byte[] pictureBinary, string mimeType, string seoFilename, string? altAttribute = null, string? titleAttribute = null,
        bool isNew = true, Reference reference = Reference.None, string objectId = "", bool validateBinary = false)
    {
        mimeType = CommonHelper.EnsureNotNull(mimeType);
        mimeType = CommonHelper.EnsureMaximumLength(mimeType, 20);

        seoFilename = CommonHelper.EnsureMaximumLength(seoFilename, 100);

        var picture = new Picture {
            PictureBinary = pictureBinary,
            MimeType = mimeType,
            SeoFilename = seoFilename,
            AltAttribute = altAttribute,
            TitleAttribute = titleAttribute,
            Reference = reference,
            ObjectId = objectId,
            IsNew = isNew
        };

        await repository.InsertAsync(picture);

        return picture;
    }

}
