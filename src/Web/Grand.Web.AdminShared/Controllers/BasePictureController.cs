using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Domain.Permissions;
using Grand.SharedKernel.Extensions;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.Pictures)]
[AutoValidateAntiforgeryToken]
public abstract class BasePictureController(
    IPictureService pictureService,
    IRequestPermissionService permissionService,
    IMediaFileStore mediaFileStore,
    MediaSettings mediaSettings)
    : BaseController
{
    [HttpPost]
    public virtual async Task<IActionResult> AsyncUpload(IFormFile file, Reference reference = Reference.None, string objectId = "")
    {
        if (file == null)
            return Json(new {
                success = false,
                message = "No file uploaded",
                downloadGuid = Guid.Empty
            });
        if (reference != Reference.None && string.IsNullOrEmpty(objectId))
            return Json(new {
                success = false,
                message = "Please save form before upload new picture",
                downloadGuid = Guid.Empty
            });

        var fileName = Path.GetFileName(file.FileName);
        var contentType = file.ContentType;
        if (!FileExtensions.GetAllowedMediaFileTypes(mediaSettings.AllowedFileTypes).IsAllowedMediaFileType(Path.GetExtension(fileName)))
            return Json(new {
                success = false,
                pictureId = "",
                imageUrl = ""
            });
        if (string.IsNullOrEmpty(contentType))
            _ = new FileExtensionContentTypeProvider().TryGetContentType(fileName, out contentType);

        var fileBinary = file.GetDownloadBits();
        var picture =
            await pictureService.InsertPicture(fileBinary, contentType, null, reference: reference,
                objectId: objectId);
        //when returning JSON the mime-type must be set to text/plain
        //otherwise some browsers will pop-up a "Save As" dialog.
        return Json(new {
            success = true,
            pictureId = picture.Id,
            imageUrl = await pictureService.GetPictureUrl(picture, 100)
        });
    }

    [HttpPost]
    public virtual async Task<IActionResult> AsyncLogoUpload(IFormFile file)
    {
        if (!await permissionService.Authorize(StandardPermission.ManageSettings))
            return Content("Access denied");

        if (file == null)
            return Json(new {
                success = false,
                message = "No file uploaded"
            });

        var fileName = Path.GetFileName(file.FileName);
        var contentType = file.ContentType;
        if (!FileExtensions.GetAllowedMediaFileTypes(mediaSettings.AllowedFileTypes).IsAllowedMediaFileType(Path.GetExtension(fileName)))
            return Json(new {
                success = false,
                message = "File no allowed"
            });

        if (string.IsNullOrEmpty(contentType))
            _ = new FileExtensionContentTypeProvider().TryGetContentType(fileName, out contentType);
        if (string.IsNullOrEmpty(contentType))
            return Json(new {
                success = false,
                message = "Unknown content type"
            });
        fileName = fileName.Replace(" ", "");
        var filepath = mediaFileStore.GetDirectoryInfo("");
        if (filepath == null)
            return Json(new {
                success = false,
                message = "Physical path not exist"
            });

        //the folder is shared by every store, so an existing file (another store's logo) is never replaced
        fileName = await FreeFileName(fileName);
        try
        {
            await using (var stream = new FileStream(mediaFileStore.Combine(filepath.PhysicalPath, fileName),
                             FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            return Json(new {
                success = true,
                imageUrl = fileName
            });
        }
        catch (Exception ex)
        {
            return Json(new {
                success = false,
                message = ex.Message
            });
        }
    }

    private async Task<string> FreeFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var candidate = fileName;
        for (var i = 1; await mediaFileStore.GetFileInfo(candidate) != null; i++)
            candidate = $"{name}-{i}{extension}";
        return candidate;
    }
}
