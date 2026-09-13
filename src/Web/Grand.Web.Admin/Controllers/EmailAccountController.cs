using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Messages;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Caching;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

// Reduced to a thin subclass of BaseEmailAccountController (ARCH-001 EmailAccount
// consolidation). Create/Edit/SendTestEmail/Delete live in the shared base; List (its grid data
// needs the Admin-only IsDefaultEmailAccount stamp) and MarkAsDefaultEmail (writes a single
// global setting, no per-store concept exists) stay here (see the design spec).
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class EmailAccountController(
    IEmailAccountViewModelService emailAccountViewModelService,
    IEmailAccountService emailAccountService,
    ITranslationService translationService,
    IAdminDataScope<EmailAccount> scope,
    ISettingService settingService,
    EmailAccountSettings emailAccountSettings,
    ICacheBase cacheBase)
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
        foreach (var eam in emailAccountModels)
            eam.IsDefaultEmailAccount = eam.Id == emailAccountSettings.DefaultEmailAccountId;

        var gridModel = new DataSourceResult {
            Data = emailAccountModels,
            Total = emailAccounts.TotalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> MarkAsDefaultEmail(string id)
    {
        var defaultEmailAccount = await EmailAccountService.GetEmailAccountById(id);
        if (defaultEmailAccount != null)
        {
            emailAccountSettings.DefaultEmailAccountId = defaultEmailAccount.Id;
            await settingService.SaveSetting(emailAccountSettings);
        }

        //now clear cache
        await cacheBase.Clear();

        return RedirectToAction("List");
    }
}
