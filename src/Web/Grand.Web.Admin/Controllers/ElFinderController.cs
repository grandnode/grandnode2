using elFinder.NetCore;
using elFinder.NetCore.Drivers.FileSystem;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.Storage.Interfaces;
using Grand.Business.Storage.Services;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
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
        #endregion

        #region Ctor

        public ElFinderController(
            IWebHostEnvironment hostingEnvironment,
            IPermissionService permissionService)
        {
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

            var connector = GetConnector();
            return await connector.ProcessAsync(Request);
        }

        private Connector GetConnector()
        {
            var driver = new FileSystemDriver();

            string absoluteUrl = UriHelper.BuildAbsolute(Request.Scheme, Request.Host);
            var uri = new Uri(absoluteUrl);

            var root = new RootVolume(
                fullPathToUpload,
                $"{uri.Scheme}://{uri.Authority}/assets/images/upload/",
                $"") {
                //IsReadOnly = !User.IsInRole("Administrators")
                IsReadOnly = false, // Can be readonly according to user's membership permission
                IsLocked = false, // If locked, files and directories cannot be deleted, renamed or moved
                Alias = "", // Beautiful name given to the root/home folder
                                 //MaxUploadSizeInKb = 2048, // Limit imposed to user uploaded file <= 2048 KB

            };

            driver.AddRoot(root);

            return new Connector(driver) {
                // This allows support for the "onlyMimes" option on the client.
                MimeDetect = MimeDetectOption.Internal
            };
        }

        #endregion
    }
}