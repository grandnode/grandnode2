using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Media;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

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
