using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Messages;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

// Reduced to a thin subclass of BaseMessageTemplateController (ARCH-001 MessageTemplate
// consolidation). Create/Edit/Delete/CopyTemplate live in the shared base; List/ListGlobal/
// ListStore stay here — Store's own two-tab split, no Admin equivalent (see the design spec).
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class MessageTemplateController(
    IMessageTemplateService messageTemplateService,
    IEmailAccountService emailAccountService,
    ILanguageService languageService,
    ITranslationService translationService,
    IMessageTokenProvider messageTokenProvider,
    IDownloadService downloadService,
    IAdminDataScope<MessageTemplate> scope,
    IContextAccessor contextAccessor)
    : BaseMessageTemplateController(messageTemplateService, emailAccountService, languageService,
        translationService, messageTokenProvider, downloadService, scope)
{
    private string CurrentStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    public IActionResult List() => View();

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> ListGlobal(DataSourceRequest command)
    {
        var allTemplates = await messageTemplateService.GetAllMessageTemplates("");
        var globalTemplates = allTemplates
            .Where(t => !t.LimitedToStores)
            .ToList();

        var total = globalTemplates.Count;
        var items = globalTemplates
            .Skip((command.Page - 1) * command.PageSize)
            .Take(command.PageSize)
            .Select(x => x.ToModel())
            .ToList();

        return Json(new DataSourceResult { Data = items, Total = total });
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> ListStore(DataSourceRequest command)
    {
        var allTemplates = await messageTemplateService.GetAllMessageTemplates("");
        var storeTemplates = allTemplates
            .Where(t => t.LimitedToStores && t.Stores.Contains(CurrentStoreId))
            .ToList();

        var total = storeTemplates.Count;
        var items = storeTemplates
            .Skip((command.Page - 1) * command.PageSize)
            .Take(command.PageSize)
            .Select(x => x.ToModel())
            .ToList();

        return Json(new DataSourceResult { Data = items, Total = total });
    }
}
