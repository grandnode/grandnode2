using elFinder.Net.AspNetCore.Extensions;
using elFinder.Net.AspNetCore.Helper;
using elFinder.Net.Core;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Media;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Admin.Services;

public class ElFinderViewModelService : IElFinderViewModelService
{
    private readonly IConnector _connector;
    private readonly IDriver _driver;
    private readonly string _fullPathToThumbs;

    private readonly string _fullPathToUpload;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;
    private readonly IMediaFileStore _mediaFileStore;
    private readonly MediaSettings _mediaSettings;
    private readonly string _urlpathUploded;
    private readonly string _urlThumb;

    private static string ImageThumbPath => Path.Combine("assets", "images", "thumbs");
    private static string ImageUploadedPath => Path.Combine("assets", "images", "uploaded");

    public ElFinderViewModelService(
        IDriver driver,
        IConnector connector,
        IHttpContextAccessor httpContextAccessor,
        IMediaFileStore mediaFileStore,
        LinkGenerator linkGenerator,
        MediaSettings mediaSettings)
    {
        _driver = driver;
        _connector = connector;
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
        _mediaFileStore = mediaFileStore;
        _mediaSettings = mediaSettings;

        if (!string.IsNullOrEmpty(_mediaSettings.FileManagerEnabledCommands))
            _connector.Options.EnabledCommands =
                _mediaSettings.FileManagerEnabledCommands.Split(',').Select(x => x.Trim()).ToList();

        if (!string.IsNullOrEmpty(_mediaSettings.FileManagerDisabledUICommands))
            _connector.Options.DisabledUICommands = _mediaSettings.FileManagerDisabledUICommands.Split(',')
                .Select(x => x.Trim()).ToList();

        var uploaded = _mediaFileStore.GetDirectoryInfo(ImageUploadedPath);
        if (uploaded == null)
        {
            _mediaFileStore.TryCreateDirectory(ImageUploadedPath);
            uploaded = _mediaFileStore.GetDirectoryInfo(ImageUploadedPath);
        }

        _fullPathToUpload = uploaded.PhysicalPath;
        var configuration = httpContextAccessor.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

        _urlpathUploded = (string.IsNullOrEmpty(configuration[CommonPath.DirectoryParam]) ? "/" : $"/{configuration[CommonPath.DirectoryParam]}/")
                          + uploaded.Path.Replace("\\", "/") + "/";

        var thumbs = _mediaFileStore.GetDirectoryInfo(ImageThumbPath);
        if (thumbs == null)
        {
            _mediaFileStore.TryCreateDirectory(ImageThumbPath);
            thumbs = _mediaFileStore.GetDirectoryInfo(ImageThumbPath);
        }

        _fullPathToThumbs = thumbs.PhysicalPath;

        _urlThumb =
            $"{_linkGenerator.GetPathByAction(_httpContextAccessor.HttpContext!, "Thumb", "ElFinder", new { area = "Admin" })}/";
    }

    public virtual async Task SetupConnectorAsync()
    {
        var volume = new Volume(_driver,
            _fullPathToUpload,
            _fullPathToThumbs,
            _urlpathUploded,
            _urlThumb) {
            Name = "Volume",
            MaxUploadConnections = 3,
            MaxUploadSizeInMb = 4,
            ObjectAttributes = new List<FilteredObjectAttribute> {
                new() {
                    FileFilter = file => NotAllowedExtensions(file.Extension),
                    ObjectFilter = obj =>
                    {
                        var extensions = Path.GetExtension(obj.FullName);
                        if (!string.IsNullOrEmpty(extensions))
                            return NotAllowedExtensions(extensions);

                        return false;
                    },
                    ShowOnly = false, Access = false, Visible = false, Write = false, Read = false
                }
            }
        };
        _connector.AddVolume(volume);
        await volume.Driver.SetupVolumeAsync(volume);
    }

    public virtual async Task<IActionResult> Connector()
    {
        await SetupConnectorAsync();
        var cmd = ConnectorHelper.ParseCommand(_httpContextAccessor.HttpContext.Request);
        if (cmd.Cmd == "upload" && cmd.Files != null && cmd.Files.Any())
            cmd.Files = cmd.Files.Where(x => !NotAllowedExtensions(Path.GetExtension(x.FileName))).ToList();

        var ccTokenSource = ConnectorHelper.RegisterCcTokenSource(_httpContextAccessor.HttpContext);
        var conResult = await _connector.ProcessAsync(cmd, ccTokenSource);
        var actionResult = conResult.ToActionResult(_httpContextAccessor.HttpContext);
        return actionResult;
    }

    public virtual async Task<IActionResult> Thumbs(string id)
    {
        await SetupConnectorAsync();

        var thumb = await _connector.GetThumbAsync(id, _httpContextAccessor.HttpContext.RequestAborted);
        var actionResult = ConnectorHelper.GetThumbResult(thumb);
        return actionResult;
    }

    protected virtual bool NotAllowedExtensions(string extensions)
    {
        var allowedFileTypes = new List<string>();
        allowedFileTypes = string.IsNullOrEmpty(_mediaSettings.AllowedFileTypes)
            ? [".gif", ".jpg", ".jpeg", ".png", ".bmp", ".webp"]
            : _mediaSettings.AllowedFileTypes.Split(',').Select(x => x.Trim().ToLowerInvariant()).ToList();

        return !allowedFileTypes.Contains(extensions);
    }
}