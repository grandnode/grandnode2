using elFinder.Net.AspNetCore.Extensions;
using elFinder.Net.AspNetCore.Helper;
using elFinder.Net.Core;
using elFinder.Net.Core.Http;
using elFinder.Net.Core.Models.Command;
using elFinder.Net.Core.Models.Response;
using elFinder.Net.Core.Models.Result;
using elFinder.Net.Core.Services.Drawing;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.Storage.Interfaces;
using Grand.Business.Storage.Services;
using Grand.SharedKernel.Extensions;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Files)]
    public class ElFinderController : BaseAdminController
    {

        #region Fields

        private Dictionary<string, string> _settings;
        private Dictionary<string, string> _languageResources;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IPermissionService _permissionService;

        private readonly IMediaFileStore _mediaFileStore;
        private string fullPathToUpload;

        private readonly IConnector _connector;
        private readonly IDriver _driver;

        #endregion

        #region Ctor

        public ElFinderController(
            IWebHostEnvironment hostingEnvironment,
            IPermissionService permissionService,
            IConnector connector, IDriver driver
            )
        {
            _connector = connector;
            _driver = driver;
            _hostingEnvironment = hostingEnvironment;
            _permissionService = permissionService;
            fullPathToUpload = Path.Combine(CommonPath.WebRootPath, Path.Combine("assets", "images", "uploaded"));
            if (!Directory.Exists(fullPathToUpload))
                Directory.CreateDirectory(fullPathToUpload);

            var fullPath = Path.Combine(CommonPath.WebRootPath, Path.Combine("assets", "images"));
            var fileStore = new FileSystemStore(fullPath);
            _mediaFileStore = new DefaultMediaFileStore(fileStore);

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
            var ccTokenSource = ConnectorHelper.RegisterCcTokenSource(HttpContext);
            var conResult = await _connector.ProcessAsync(cmd, ccTokenSource);
            var actionResult = conResult.ToActionResult(HttpContext);
            return actionResult;
        }

        private async Task SetupConnectorAsync()
        {
            var volume = new Volume(_driver,
                fullPathToUpload,
                Path.GetTempPath(),
                "/assets/images/uploaded",
                "/assets/images/thumb") {
                StartDirectory = fullPathToUpload,
                Name = $"Volume",
                MaxUploadConnections = 3
            };

            _connector.AddVolume(volume);
            await volume.Driver.SetupVolumeAsync(volume);

            // Events
            _driver.OnBeforeMove += (sender, args) =>
            {
                Console.WriteLine("Move: " + args.FileSystem.FullName);
                Console.WriteLine("To: " + args.NewDest);
            };
        }

        //private Connector GetConnector()
        //{
        //    var driver = new FileSystemDriver();

        //    string absoluteUrl = UriHelper.BuildAbsolute(Request.Scheme, Request.Host);
        //    var uri = new Uri(absoluteUrl);

        //    var root = new RootVolume(
        //        fullPathToUpload,
        //        $"{uri.Scheme}://{uri.Authority}/assets/images/upload/",
        //        $"") {
        //        //IsReadOnly = !User.IsInRole("Administrators")
        //        IsReadOnly = false, // Can be readonly according to user's membership permission
        //        IsLocked = false, // If locked, files and directories cannot be deleted, renamed or moved
        //        Alias = "", // Beautiful name given to the root/home folder
        //                         //MaxUploadSizeInKb = 2048, // Limit imposed to user uploaded file <= 2048 KB

        //    };

        //    driver.AddRoot(root);

        //    return new Connector(driver) {
        //        // This allows support for the "onlyMimes" option on the client.
        //        MimeDetect = MimeDetectOption.Internal
        //    };
        //}

        #endregion
    }

}