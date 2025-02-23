using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Permissions;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Http;
using Grand.SharedKernel.Extensions;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.Pictures)]
public class PictureController : BaseAdminController
{
    private readonly IMediaFileStore _mediaFileStore;

    private readonly MediaSettings _mediaSettings;
    private readonly IPermissionService _permissionService;
    private readonly IPictureService _pictureService;

    public PictureController(
        IPictureService pictureService,
        IPermissionService permissionService,
        IMediaFileStore mediaFileStore,
        MediaSettings mediaSettings)
    {
        _pictureService = pictureService;
        _permissionService = permissionService;
        _mediaFileStore = mediaFileStore;
        _mediaSettings = mediaSettings;
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
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
        if (!FileExtensions.GetAllowedMediaFileTypes(_mediaSettings.AllowedFileTypes).IsAllowedMediaFileType(Path.GetExtension(fileName)))
            return Json(new {
                success = false,
                pictureId = "",
                imageUrl = ""
            });
        if (string.IsNullOrEmpty(contentType))
            _ = new FileExtensionContentTypeProvider().TryGetContentType(fileName, out contentType);

        var fileBinary = file.GetDownloadBits();
        var picture =
            await _pictureService.InsertPicture(fileBinary, contentType, null, reference: reference,
                objectId: objectId);
        //when returning JSON the mime-type must be set to text/plain
        //otherwise some browsers will pop-up a "Save As" dialog.
        return Json(new {
            success = true,
            pictureId = picture.Id,
            imageUrl = await _pictureService.GetPictureUrl(picture, 100)
        });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public virtual async Task<IActionResult> AsyncLogoUpload(IFormFile file)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageSettings))
            return Content("Access denied");

        if (file == null)
            return Json(new {
                success = false,
                message = "No file uploaded"
            });

        var fileName = Path.GetFileName(file.FileName);
        var contentType = file.ContentType;
        if (!FileExtensions.GetAllowedMediaFileTypes(_mediaSettings.AllowedFileTypes).IsAllowedMediaFileType(Path.GetExtension(fileName)))
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
        var fileInfo = await _mediaFileStore.GetFileInfo(fileName);
        if (fileInfo == null)
            try
            {
                var filepath = _mediaFileStore.GetDirectoryInfo("");
                if (filepath != null)
                {
                    await using (var stream = new FileStream(_mediaFileStore.Combine(filepath.PhysicalPath, fileName),
                                     FileMode.OpenOrCreate))
                    {
                        await file.CopyToAsync(stream);
                    }

                    return Json(new {
                        success = true,
                        imageUrl = fileName
                    });
                }

                return Json(new {
                    success = false,
                    message = "Physical path not exist"
                });
            }
            catch (Exception ex)
            {
                return Json(new {
                    success = false,
                    message = ex.Message
                });
            }

        {
            var filepath = _mediaFileStore.GetDirectoryInfo("");
            if (filepath != null)
            {
                await using (var stream = new FileStream(_mediaFileStore.Combine(filepath.PhysicalPath, fileName),
                                 FileMode.OpenOrCreate))
                {
                    await file.CopyToAsync(stream);
                }

                return Json(new {
                    success = true,
                    imageUrl = fileName
                });
            }

            return Json(new {
                success = false,
                message = "Physical path not exist"
            });
        }
    }
}