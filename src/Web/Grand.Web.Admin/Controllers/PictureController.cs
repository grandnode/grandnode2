using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Media;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

// Reduced to a thin subclass of BasePictureController (ARCH-001). Both actions live in the shared
// base; this class only supplies Admin's DI wiring plus the attributes that used to arrive
// transitively via BaseAdminController - BasePictureController can't inherit any single host's base
// controller (it's shared across Admin/Store), so each subclass restates its own host's attribute
// set explicitly.
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class PictureController(
    IPictureService pictureService,
    IPermissionService permissionService,
    IMediaFileStore mediaFileStore,
    MediaSettings mediaSettings)
    : BasePictureController(pictureService, permissionService, mediaFileStore, mediaSettings)
{
}
