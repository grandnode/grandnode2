using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Media;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

// Reduced to a thin subclass of BasePictureController (ARCH-001). Both actions live in the shared
// base; this class only supplies Store's DI wiring plus the attributes that used to arrive
// transitively via BaseStoreController - BasePictureController can't inherit any single host's base
// controller (it's shared across Admin/Store), so each subclass restates its own host's attribute
// set explicitly.
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class PictureController(
    IPictureService pictureService,
    IPermissionService permissionService,
    IMediaFileStore mediaFileStore,
    MediaSettings mediaSettings)
    : BasePictureController(pictureService, permissionService, mediaFileStore, mediaSettings)
{
}
