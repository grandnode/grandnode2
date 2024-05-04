using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Media;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.Files)]
public class DownloadController : BaseAdminController
{
    private readonly IDownloadService _downloadService;
    private readonly IWorkContext _workContext;

    public DownloadController(IDownloadService downloadService, IWorkContext workContext)
    {
        _downloadService = downloadService;
        _workContext = workContext;
    }

    public async Task<IActionResult> DownloadFile(Guid downloadGuid)
    {
        var download = await _downloadService.GetDownloadByGuid(downloadGuid);
        if (download == null)
            return Content("No download record found with the specified id");

        if (download.UseDownloadUrl)
            return new RedirectResult(download.DownloadUrl);

        //use stored data
        if (download.DownloadBinary == null)
            return Content($"Download data is not available any more. Download GD={download.Id}");

        var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : download.Id;
        var contentType = !string.IsNullOrWhiteSpace(download.ContentType)
            ? download.ContentType
            : "application/octet-stream";
        return new FileContentResult(download.DownloadBinary, contentType) {
            FileDownloadName = fileName + download.Extension
        };
    }

    [HttpPost]

    //do not validate request token (XSRF)
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SaveDownloadUrl(string downloadUrl, DownloadType downloadType = DownloadType.None,
        string referenceId = "")
    {
        if (string.IsNullOrEmpty(downloadUrl)) return Json(new { success = false, error = "URL can't be empty" });
        //insert
        var download = new Download {
            DownloadGuid = Guid.NewGuid(),
            UseDownloadUrl = true,
            DownloadUrl = downloadUrl,
            DownloadType = downloadType,
            ReferenceId = referenceId
        };
        await _downloadService.InsertDownload(download);

        return Json(new { downloadId = download.Id, success = true });
    }

    [DisableRequestSizeLimit]
    [HttpPost]
    //do not validate request token (XSRF)
    [IgnoreAntiforgeryToken]
    public virtual async Task<IActionResult> AsyncUpload(DownloadType downloadType = DownloadType.None,
        string referenceId = "")
    {
        var form = await HttpContext.Request.ReadFormAsync();
        var httpPostedFile = form.Files.FirstOrDefault();
        if (httpPostedFile == null)
            return Json(new {
                success = false,
                message = "No file uploaded",
                downloadGuid = Guid.Empty
            });

        var fileBinary = httpPostedFile.GetDownloadBits();

        var qqFileNameParameter = "qqfilename";
        var fileName = httpPostedFile.FileName;
        if (string.IsNullOrEmpty(fileName) && form.ContainsKey(qqFileNameParameter))
            fileName = form[qqFileNameParameter].ToString();

        fileName = Path.GetFileName(fileName);

        var contentType = httpPostedFile.ContentType;

        var fileExtension = Path.GetExtension(fileName);
        if (!string.IsNullOrEmpty(fileExtension))
            fileExtension = fileExtension.ToLowerInvariant();


        var download = new Download {
            DownloadGuid = Guid.NewGuid(),
            CustomerId = _workContext.CurrentCustomer.Id,
            UseDownloadUrl = false,
            DownloadUrl = "",
            DownloadBinary = fileBinary,
            ContentType = contentType,
            Filename = Path.GetFileNameWithoutExtension(fileName),
            Extension = fileExtension,
            DownloadType = downloadType,
            ReferenceId = referenceId
        };
        await _downloadService.InsertDownload(download);

        //when returning JSON the mime-type must be set to text/plain
        //otherwise some browsers will pop-up a "Save As" dialog.
        return Json(new {
            success = true,
            downloadId = download.Id,
            downloadUrl = Url.Action("DownloadFile",
                new { downloadGuid = download.DownloadGuid, area = Constants.AreaAdmin })
        });
    }
}