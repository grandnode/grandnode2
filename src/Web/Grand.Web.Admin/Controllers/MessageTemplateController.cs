using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Messages;
using Grand.Domain.Permissions;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Messages;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Controllers;

// Reduced to a thin subclass of BaseMessageTemplateController (ARCH-001 MessageTemplate
// consolidation). Create/Edit/Delete/CopyTemplate live in the shared base; List and its
// grid-data action stay here because Admin's single store-filterable grid is a genuinely
// different UI/workflow decision from Store's two-tab split (see the design spec) - same
// reasoning as other kept-List thin subclasses.
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class MessageTemplateController(
    IMessageTemplateService messageTemplateService,
    IEmailAccountService emailAccountService,
    ILanguageService languageService,
    ITranslationService translationService,
    IMessageTokenProvider messageTokenProvider,
    IDownloadService downloadService,
    IAdminDataScope<MessageTemplate> scope,
    IStoreService storeService)
    : BaseMessageTemplateController(messageTemplateService, emailAccountService, languageService,
        translationService, messageTokenProvider, downloadService, scope)
{
    public async Task<IActionResult> List()
    {
        var model = new MessageTemplateListModel();
        model.AvailableStores.Add(new SelectListItem
            { Text = translationService.GetResource("Admin.Common.All"), Value = "" });
        foreach (var s in await storeService.GetAllStores())
            model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command, MessageTemplateListModel model)
    {
        var messageTemplates = await messageTemplateService.GetAllMessageTemplates(
            model.SearchStoreId,
            keywords: model.Name,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize);

        var items = new List<MessageTemplateModel>();
        foreach (var x in messageTemplates)
        {
            var templateModel = x.ToModel();
            var stores = (await storeService.GetAllStores())
                .Where(s => !x.LimitedToStores || templateModel.Stores.Contains(s.Id))
                .ToList();
            for (var i = 0; i < stores.Count; i++)
            {
                templateModel.ListOfStores += stores[i].Shortcut;
                if (i != stores.Count - 1)
                    templateModel.ListOfStores += ", ";
            }

            items.Add(templateModel);
        }

        return Json(new DataSourceResult {
            Data = items,
            Total = messageTemplates.TotalCount
        });
    }
}
