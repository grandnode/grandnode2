using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.Storage.Extensions;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Pictures)]
    public partial class PictureController : BaseAdminController
    {
        private readonly IPictureService _pictureService;
        private readonly IPermissionService _permissionService;
        private readonly IMediaFileStore _mediaFileStore;

        private readonly MediaSettings _mediaSettings;

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

        [NonAction]
        protected virtual IList<string> GetAllowedFileTypes()
        {
            if (string.IsNullOrEmpty(_mediaSettings.AllowedFileTypes))
                return new List<string> { ".gif", ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
            else
                return _mediaSettings.AllowedFileTypes.Split(',');
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> AsyncUpload(Reference reference = Reference.None, string objectId = "")
        {
            var form = await HttpContext.Request.ReadFormAsync();
            var httpPostedFile = form.Files.FirstOrDefault();
            if (httpPostedFile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "No file uploaded",
                    downloadGuid = Guid.Empty,
                });
            }
            if (reference != Reference.None && string.IsNullOrEmpty(objectId))
                return Json(new
                {
                    success = false,
                    message = "Please save form before upload new picture",
                    downloadGuid = Guid.Empty,
                });

            var qqFileNameParameter = "qqfilename";
            var fileName = httpPostedFile.FileName;
            if (String.IsNullOrEmpty(fileName) && form.ContainsKey(qqFileNameParameter))
                fileName = form[qqFileNameParameter].ToString();
            //remove path (passed in IE)
            fileName = Path.GetFileName(fileName);

            var contentType = httpPostedFile.ContentType;

            var fileExtension = Path.GetExtension(fileName);
            if (!String.IsNullOrEmpty(fileExtension))
                fileExtension = fileExtension.ToLowerInvariant();

            if (!GetAllowedFileTypes().Contains(fileExtension))
            {
                return Json(new
                {
                    success = false,
                    pictureId = "",
                    imageUrl = ""
                });
            }

            //contentType is not always available 
            //that's why we manually update it here
            //http://www.sfsu.edu/training/mimetype.htm
            if (String.IsNullOrEmpty(contentType))
            {
                switch (fileExtension)
                {
                    case ".bmp":
                        contentType = "image/bmp";
                        break;
                    case ".gif":
                        contentType = "image/gif";
                        break;
                    case ".jpeg":
                    case ".jpg":
                    case ".jpe":
                    case ".jfif":
                    case ".pjpeg":
                    case ".pjp":
                        contentType = "image/jpeg";
                        break;
                    case ".png":
                        contentType = "image/png";
                        break;
                    case ".tiff":
                    case ".tif":
                        contentType = "image/tiff";
                        break;
                    default:
                        break;
                }
            }

            var fileBinary = httpPostedFile.GetDownloadBits();
            var picture = await _pictureService.InsertPicture(fileBinary, contentType, null, reference: reference, objectId: objectId);
            //when returning JSON the mime-type must be set to text/plain
            //otherwise some browsers will pop-up a "Save As" dialog.
            return Json(new
            {
                success = true,
                pictureId = picture.Id,
                imageUrl = await _pictureService.GetPictureUrl(picture, 100)
            });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> AsyncLogoUpload()
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageSettings))
                return Content("Access denied");

            var form = await HttpContext.Request.ReadFormAsync();
            var httpPostedFile = form.Files.FirstOrDefault();
            if (httpPostedFile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "No file uploaded",
                });
            }


            var qqFileNameParameter = "qqfilename";
            var fileName = httpPostedFile.FileName;
            if (String.IsNullOrEmpty(fileName) && form.ContainsKey(qqFileNameParameter))
                fileName = form[qqFileNameParameter].ToString();

            fileName = Path.GetFileName(fileName);

            var contentType = httpPostedFile.ContentType;

            var fileExtension = Path.GetExtension(fileName);
            if (!String.IsNullOrEmpty(fileExtension))
                fileExtension = fileExtension.ToLowerInvariant();

            if (!GetAllowedFileTypes().Contains(fileExtension))
            {
                return Json(new
                {
                    success = false,
                    message = "File no allowed"
                });
            }

            //contentType is not always available 
            //that's why we manually update it here
            //http://www.sfsu.edu/training/mimetype.htm
            if (String.IsNullOrEmpty(contentType))
            {
                switch (fileExtension)
                {
                    case ".bmp":
                        contentType = "image/bmp";
                        break;
                    case ".gif":
                        contentType = "image/gif";
                        break;
                    case ".jpeg":
                    case ".jpg":
                    case ".jpe":
                    case ".jfif":
                    case ".pjpeg":
                    case ".pjp":
                        contentType = "image/jpeg";
                        break;
                    case ".png":
                        contentType = "image/png";
                        break;
                    case ".tiff":
                    case ".tif":
                        contentType = "image/tiff";
                        break;
                    default:
                        break;
                }
            }
            if (String.IsNullOrEmpty(contentType))
            {
                return Json(new
                {
                    success = false,
                    message = "Unknown content type"
                });
            }
            fileName = fileName.Replace(" ", "");
            var fileInfo = await _mediaFileStore.GetFileInfo(fileName);
            if (fileInfo == null)
            {
                try
                {
                    var filepath = _mediaFileStore.GetDirectoryInfo("");
                    if (filepath != null)
                    {
                        using (var stream = new FileStream(_mediaFileStore.Combine(filepath.PhysicalPath, fileName), FileMode.OpenOrCreate))
                        {
                            httpPostedFile.CopyTo(stream);
                        }
                        return Json(new
                        {
                            success = true,
                            imageUrl = fileName
                        });
                    }
                    else
                        return Json(new
                        {
                            success = false,
                            message = "Physical path not exist"
                        });
                }
                catch (Exception ex)
                {
                    return Json(new
                    {
                        success = false,
                        message = ex.Message
                    });
                }
            }
            return Json(new
            {
                success = false,
                message = "This filename exists"
            });


        }
    }
}
