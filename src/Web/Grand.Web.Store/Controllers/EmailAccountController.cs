using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Messages;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

// Reduced to a thin subclass of BaseEmailAccountController (ARCH-001 EmailAccount
// consolidation). Create/Edit/SendTestEmail/Delete live in the shared base; List stays here —
// Store has no MarkAsDefaultEmail equivalent (see the design spec).
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class EmailAccountController(
    IEmailAccountViewModelService emailAccountViewModelService,
    IEmailAccountService emailAccountService,
    ITranslationService translationService,
    IAdminDataScope<EmailAccount> scope)
    : BaseEmailAccountController(emailAccountViewModelService, emailAccountService, translationService, scope)
{
    public IActionResult List()
    {
        return View();
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> List(DataSourceRequest command)
    {
        var emailAccounts = await EmailAccountService.GetAllEmailAccounts(Scope.DefaultStoreId ?? "",
            pageIndex: command.Page - 1, pageSize: command.PageSize);
        var emailAccountModels = emailAccounts.Select(x => x.ToModel()).ToList();

        var gridModel = new DataSourceResult {
            Data = emailAccountModels,
            Total = emailAccounts.TotalCount
        };

        return Json(gridModel);
    }
}
