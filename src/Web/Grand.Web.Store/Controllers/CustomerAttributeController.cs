using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
[PermissionAuthorize(PermissionSystemName.CustomerAttributes)]
public class CustomerAttributeController(
    ICustomerAttributeService customerAttributeService,
    ICustomerAttributeViewModelService customerAttributeViewModelService,
    ILanguageService languageService,
    ITranslationService translationService,
    IAdminDataScope<CustomerAttribute> scope)
    : BaseCustomerAttributeController(customerAttributeService, customerAttributeViewModelService,
        languageService, translationService, scope)
{
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public override async Task<IActionResult> ValueEditPopup(string id, string customerAttributeId) =>
        await base.ValueEditPopup(id, customerAttributeId);
}

