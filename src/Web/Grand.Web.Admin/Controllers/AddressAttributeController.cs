using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Common;
using Grand.Domain.Permissions;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class AddressAttributeController(
    IAddressAttributeService addressAttributeService,
    IAddressAttributeViewModelService addressAttributeViewModelService,
    ILanguageService languageService,
    ITranslationService translationService,
    IAdminDataScope<AddressAttribute> scope)
    : BaseAddressAttributeController(addressAttributeService, addressAttributeViewModelService,
        languageService, translationService, scope)
{
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public override async Task<IActionResult> ValueCreatePopup(string addressAttributeId) =>
        await base.ValueCreatePopup(addressAttributeId);

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [HttpPost]
    public override async Task<IActionResult> ValueCreatePopup(AddressAttributeValueModel model) =>
        await base.ValueCreatePopup(model);

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public override async Task<IActionResult> ValueEditPopup(string id, string addressAttributeId) =>
        await base.ValueEditPopup(id, addressAttributeId);
}