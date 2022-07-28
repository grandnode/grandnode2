using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.HtmlEditor)]
    public class ElFinderController : BaseAdminController
    {

        #region Fields

        private readonly IPermissionService _permissionService;
        private readonly IElFinderViewModelService _elFinderViewModelService;

        #endregion

        #region Ctor

        public ElFinderController(
            IPermissionService permissionService,
            IElFinderViewModelService elFinderViewModelService
            )
        {
            _elFinderViewModelService = elFinderViewModelService;
            _permissionService = permissionService;
        }

        #endregion

        #region Methods

        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> Connector()
        {
            if (!await _permissionService.Authorize(StandardPermission.HtmlEditorManagePictures))
                return new JsonResult(new { error = "You don't have required permission" });

            return await _elFinderViewModelService.Connector();
        }

        public async Task<IActionResult> Thumb(string id)
        {
            if (!await _permissionService.Authorize(StandardPermission.HtmlEditorManagePictures))
                return new JsonResult(new { error = "You don't have required permission" });

            return await _elFinderViewModelService.Thumbs(id);
        }

        #endregion
    }

}