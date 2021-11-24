using elFinder.Net.AspNetCore.Extensions;
using elFinder.Net.AspNetCore.Helper;
using elFinder.Net.Core;
using Grand.Domain.Media;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Services
{
    public partial class ElFinderViewModelService : IElFinderViewModelService
    {
        private readonly IDriver _driver;
        private readonly IConnector _connector;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly LinkGenerator _linkGenerator;
        private readonly MediaSettings _mediaSettings;

        private readonly string _fullPathToUpload;
        private readonly string _fullPathToThumbs;
        private readonly string _urlThumb;
        private readonly string _urlpathUploded;

        public ElFinderViewModelService(
            IDriver driver,
            IConnector connector,
            IHttpContextAccessor httpContextAccessor,
            LinkGenerator linkGenerator,
            MediaSettings mediaSettings)
        {
            _driver = driver;
            _connector = connector;
            _httpContextAccessor = httpContextAccessor;
            _linkGenerator = linkGenerator;

            _mediaSettings = mediaSettings;

            _connector.Options.EnabledCommands = _mediaSettings.FileManagerEnabledCommands.Split(',').Select(x => x.Trim()).ToList();
            _connector.Options.DisabledUICommands = _mediaSettings.FileManagerDisabledUICommands.Split(',').Select(x => x.Trim()).ToList();

            _urlpathUploded = "/assets/images/uploaded/";

            _fullPathToUpload = Path.Combine(CommonPath.WebRootPath, Path.Combine("assets", "images", "uploaded"));
            if (!Directory.Exists(_fullPathToUpload))
                Directory.CreateDirectory(_fullPathToUpload);

            _fullPathToThumbs = Path.Combine(CommonPath.WebRootPath, Path.Combine("assets", "images", "thumbs"));
            if (!Directory.Exists(_fullPathToThumbs))
                Directory.CreateDirectory(_fullPathToThumbs);

            _urlThumb = $"{_linkGenerator.GetPathByAction(_httpContextAccessor.HttpContext, "Thumb", "ElFinder", new { area = "Admin" })}/";
        }

        protected virtual bool NotAllowedExtensions(string extensions)
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

        public virtual async Task SetupConnectorAsync()
        {
            var volume = new Volume(_driver,
                _fullPathToUpload,
                _fullPathToThumbs,
                _urlpathUploded,
                _urlThumb) {
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
    }
}
