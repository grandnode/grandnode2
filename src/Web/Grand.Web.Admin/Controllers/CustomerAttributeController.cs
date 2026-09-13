using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
[PermissionAuthorize(PermissionSystemName.Settings)]
public class CustomerAttributeController(
    ICustomerAttributeService customerAttributeService,
    ICustomerAttributeViewModelService customerAttributeViewModelService,
    ILanguageService languageService,
    ITranslationService translationService,
    IAdminDataScope<CustomerAttribute> scope)
    : BaseCustomerAttributeController(customerAttributeService, customerAttributeViewModelService,
        languageService, translationService, scope)
{
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public override async Task<IActionResult> ValueEditPopup(string id, string customerAttributeId) =>
        await base.ValueEditPopup(id, customerAttributeId);
}
