using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.FileManager;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

/// <summary>The file manager of the rich text editor ("Manage pictures"); every answer is JSON</summary>
[PermissionAuthorize(PermissionSystemName.HtmlEditor)]
public class FileManagerController : BaseAdminController
{
    #region Ctor

    public FileManagerController(
        IFileManagerService fileManagerService,
        IRequestPermissionService permissionService,
        IRequestTranslationService translationService)
    {
        _fileManagerService = fileManagerService;
        _permissionService = permissionService;
        _translationService = translationService;
    }

    #endregion

    #region Fields

    private readonly IFileManagerService _fileManagerService;
    private readonly IRequestPermissionService _permissionService;
    private readonly IRequestTranslationService _translationService;

    #endregion

    #region Methods

    [HttpGet]
    public async Task<IActionResult> List(string path)
    {
        if (!await CanManage())
            return Denied();

        var listing = _fileManagerService.List(path);
        if (listing.Error != FileManagerError.None)
            return Failed(listing.Error);

        return Json(new {
            success = true,
            path = listing.Path,
            crumbs = listing.Crumbs.Select(c => new { name = c.Name, path = c.Path }),
            entries = listing.Entries.Select(ToJson)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Upload(string path)
    {
        if (!await CanManage())
            return Denied();

        var files = Request.HasFormContentType
            ? Request.Form.Files.Select(f => new FileManagerUpload(f.FileName, f.Length, f.OpenReadStream)).ToList()
            : [];
        var result = await _fileManagerService.Upload(path, files);
        if (result.Error != FileManagerError.None)
            return Failed(result.Error);

        return Json(new {
            success = true,
            uploaded = result.Uploaded.Select(ToJson),
            rejected = result.Rejected.Select(r => new { name = r.Name, message = Message(r.Error) })
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateFolder(string path, string name)
    {
        if (!await CanManage())
            return Denied();

        return Answer(await _fileManagerService.CreateFolder(path, name));
    }

    [HttpPost]
    public async Task<IActionResult> Rename(string path, string name)
    {
        if (!await CanManage())
            return Denied();

        return Answer(await _fileManagerService.Rename(path, name));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string path)
    {
        if (!await CanManage())
            return Denied();

        return Answer(await _fileManagerService.Delete(path));
    }

    #endregion

    #region Utilities

    private Task<bool> CanManage()
    {
        return _permissionService.Authorize(StandardPermission.HtmlEditorManagePictures);
    }

    private IActionResult Denied()
    {
        return Json(new { success = false, message = _translationService.GetResource("Admin.FileManager.Error.AccessDenied") });
    }

    private IActionResult Answer(FileManagerResult result)
    {
        return result.Error == FileManagerError.None
            ? Json(new { success = true, entry = result.Entry == null ? null : ToJson(result.Entry) })
            : Failed(result.Error);
    }

    //the panel's serializer keeps C# names (PascalCase) for the grids; the file manager script reads camelCase
    private static object ToJson(FileManagerEntry entry)
    {
        return new {
            name = entry.Name,
            path = entry.Path,
            isFolder = entry.IsFolder,
            url = entry.Url,
            kind = entry.Kind,
            size = entry.Size,
            modifiedUtc = entry.ModifiedUtc
        };
    }

    private IActionResult Failed(FileManagerError error)
    {
        return Json(new { success = false, message = Message(error) });
    }

    private string Message(FileManagerError error)
    {
        var message = _translationService.GetResource($"Admin.FileManager.Error.{error}");
        return error == FileManagerError.TooLarge
            ? string.Format(message, FileManagerService.MaxUploadBytes / (1024 * 1024))
            : message;
    }

    #endregion
}
