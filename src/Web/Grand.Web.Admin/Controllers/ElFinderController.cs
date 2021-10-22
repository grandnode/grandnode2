using elFinder.Net.AspNetCore.Extensions;
using elFinder.Net.AspNetCore.Helper;
using elFinder.Net.Core;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Domain.Media;
using Grand.SharedKernel.Extensions;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Files)]
    public class ElFinderController : BaseAdminController
    {

        #region Fields

        private readonly IPermissionService _permissionService;
        private readonly IConnector _connector;
        private readonly IDriver _driver;
        private readonly MediaSettings _mediaSettings;

        private readonly string _fullPathToUpload;
        private readonly string _fullPathToThumbs;
        private readonly string _pathToUpload;

        #endregion

        #region Ctor

        public ElFinderController(
            IPermissionService permissionService,
            IConnector connector,
            IDriver driver,
            MediaSettings mediaSettings
            )
        {
            _mediaSettings = mediaSettings;
            _driver = driver;
            _permissionService = permissionService;
            _connector = connector;
            _connector.Options.EnabledCommands = _mediaSettings.FileManagerEnabledCommands.Split(',').Select(x => x.Trim()).ToList();
            _connector.Options.DisabledUICommands = _mediaSettings.FileManagerDisabledUICommands.Split(',').Select(x => x.Trim()).ToList();

            _pathToUpload = Path.Combine("assets", "images", "uploaded");

            _fullPathToUpload = Path.Combine(CommonPath.WebRootPath, _pathToUpload);
            if (!Directory.Exists(_fullPathToUpload))
                Directory.CreateDirectory(_fullPathToUpload);

            _fullPathToThumbs = Path.Combine(CommonPath.WebRootPath, Path.Combine("assets", "images", "thumbs"));
            if (!Directory.Exists(_fullPathToThumbs))
                Directory.CreateDirectory(_fullPathToThumbs);

        }

        #endregion

        #region Methods

        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> Connector()
        {
            if (!await _permissionService.Authorize(StandardPermission.HtmlEditorManagePictures))
                return new JsonResult(new { error = "You don't have required permission" });

            await SetupConnectorAsync();
            var cmd = ConnectorHelper.ParseCommand(Request);
            if (cmd.Cmd == "upload" && cmd.Files != null && cmd.Files.Any())
                cmd.Files = cmd.Files.Where(x => !NotAllowedExtensions(Path.GetExtension(x.FileName))).ToList();

            var ccTokenSource = ConnectorHelper.RegisterCcTokenSource(HttpContext);
            var conResult = await _connector.ProcessAsync(cmd, ccTokenSource);
            var actionResult = conResult.ToActionResult(HttpContext);
            return actionResult;
        }

        public async Task<IActionResult> Thumb(string id)
        {
            if (!await _permissionService.Authorize(StandardPermission.HtmlEditorManagePictures))
                return new JsonResult(new { error = "You don't have required permission" });

            await SetupConnectorAsync();
            var thumb = await _connector.GetThumbAsync(id, HttpContext.RequestAborted);
            var actionResult = ConnectorHelper.GetThumbResult(thumb);
            return actionResult;
        }

        private async Task SetupConnectorAsync()
        {
            var volume = new Volume(_driver,
                _fullPathToUpload,
                _fullPathToThumbs,
                "/assets/images/uploaded/",
                $"{Url.Action("Thumb")}/"
                ) {
                Name = "Volume",
                MaxUploadConnections = 3
            };
            volume.ObjectAttributes = new List<FilteredObjectAttribute>() {
                new FilteredObjectAttribute()
                {
                    FileFilter = (file) => {
                        return NotAllowedExtensions(file.Extension);
                    },
                    ObjectFilter = (obj) =>
                    {
                        var extensions = Path.GetExtension(obj.FullName);
                        if(!string.IsNullOrEmpty(extensions))
                            return NotAllowedExtensions(extensions);


                        return false;
                    },
                    ShowOnly = false, Access = false, Visible = false, Write = false, Read = false
                },
            };
            _connector.AddVolume(volume);
            await volume.Driver.SetupVolumeAsync(volume);

        }

        private bool NotAllowedExtensions(string extensions)
        {
            var allowedFileTypes = new List<string>();
            if (string.IsNullOrEmpty(_mediaSettings.AllowedFileTypes))
                allowedFileTypes = new List<string> { ".gif", ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
            else
                allowedFileTypes = _mediaSettings.AllowedFileTypes.Split(',').Select(x => x.Trim().ToLowerInvariant()).ToList();

            if (allowedFileTypes.Contains(extensions))
                return false;

            return true;
        }

        #endregion
    }

}