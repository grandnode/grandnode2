using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.SharedKernel;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Models.Messages;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.MessageTemplates)]
public class MessageTemplateController(
    IMessageTemplateService messageTemplateService,
    IEmailAccountService emailAccountService,
    ILanguageService languageService,
    ITranslationService translationService,
    IMessageTokenProvider messageTokenProvider,
    IDownloadService downloadService,
    IContextAccessor contextAccessor) : BaseStoreController
{
    private string CurrentStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public IActionResult List()
    {
        return View();
    }

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

        var gridModel = new DataSourceResult {
            Data = items,
            Total = total
        };

        return Json(gridModel);
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

        var gridModel = new DataSourceResult {
            Data = items,
            Total = total
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = new MessageTemplateModel {
            AllowedTokens = messageTokenProvider.GetListOfAllowedTokens()
        };

        foreach (var ea in await emailAccountService.GetAllEmailAccounts(CurrentStoreId))
            model.AvailableEmailAccounts.Add(ea.ToModel());

        await AddLocales(languageService, model.Locales);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(MessageTemplateModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            // Prevent duplicate: check only for store-specific templates with this name for the current store.
            // Pass keywords to pre-filter by name at the DB level, then verify exact match and store ownership.
            var existingStoreTemplate = (await messageTemplateService.GetAllMessageTemplates("", keywords: model.Name))
                .FirstOrDefault(t =>
                    t.Name == model.Name &&
                    t.LimitedToStores &&
                    t.Stores.Contains(CurrentStoreId));
            if (existingStoreTemplate != null)
            {
                ModelState.AddModelError("Name", translationService.GetResource("Admin.Content.MessageTemplates.Fields.Name.AlreadyExists"));
                model.HasAttachedDownload = !string.IsNullOrEmpty(model.AttachedDownloadId);
                model.AllowedTokens = messageTokenProvider.GetListOfAllowedTokens();
                foreach (var ea in await emailAccountService.GetAllEmailAccounts(CurrentStoreId))
                    model.AvailableEmailAccounts.Add(ea.ToModel());
                return View(model);
            }

            var messageTemplate = model.ToEntity();
            if (!model.HasAttachedDownload)
                messageTemplate.AttachedDownloadId = "";
            if (model.SendImmediately)
                messageTemplate.DelayBeforeSend = null;

            // Assign to the current store
            messageTemplate.LimitedToStores = true;
            messageTemplate.Stores = [CurrentStoreId];

            await messageTemplateService.InsertMessageTemplate(messageTemplate);

            Success(translationService.GetResource("Admin.Content.MessageTemplates.AddNew"));

            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = messageTemplate.Id });
            }

            return RedirectToAction("List");
        }

        model.HasAttachedDownload = !string.IsNullOrEmpty(model.AttachedDownloadId);
        model.AllowedTokens = messageTokenProvider.GetListOfAllowedTokens();
        foreach (var ea in await emailAccountService.GetAllEmailAccounts(CurrentStoreId))
            model.AvailableEmailAccounts.Add(ea.ToModel());

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var messageTemplate = await messageTemplateService.GetMessageTemplateById(id);
        if (messageTemplate == null)
            return RedirectToAction("List");

        // Block access to templates that belong to a different store
        if (messageTemplate.LimitedToStores && !messageTemplate.Stores.Contains(CurrentStoreId))
            return RedirectToAction("List");

        // Global templates (LimitedToStores=false) are shown read-only
        ViewBag.IsReadOnly = !messageTemplate.LimitedToStores;

        var model = messageTemplate.ToModel();
        model.SendImmediately = !model.DelayBeforeSend.HasValue;
        model.HasAttachedDownload = !string.IsNullOrEmpty(model.AttachedDownloadId);
        model.AllowedTokens = messageTokenProvider.GetListOfAllowedTokens();

        foreach (var ea in await emailAccountService.GetAllEmailAccounts(CurrentStoreId))
            model.AvailableEmailAccounts.Add(ea.ToModel());

        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.BccEmailAddresses = messageTemplate.GetTranslation(x => x.BccEmailAddresses, languageId, false);
            locale.Subject = messageTemplate.GetTranslation(x => x.Subject, languageId, false);
            locale.Body = messageTemplate.GetTranslation(x => x.Body, languageId, false);
            locale.EmailAccountId = messageTemplate.GetTranslation(x => x.EmailAccountId, languageId, false);
        });

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(MessageTemplateModel model, bool continueEditing)
    {
        var messageTemplate = await messageTemplateService.GetMessageTemplateById(model.Id);
        if (messageTemplate == null)
            return RedirectToAction("List");

        if (!messageTemplate.LimitedToStores || !messageTemplate.Stores.Contains(CurrentStoreId))
            return RedirectToAction("List");

        var prevAttachment = messageTemplate.AttachedDownloadId;

        if (ModelState.IsValid)
        {
            messageTemplate = model.ToEntity(messageTemplate);
            if (!model.HasAttachedDownload)
                messageTemplate.AttachedDownloadId = "";
            if (model.SendImmediately)
                messageTemplate.DelayBeforeSend = null;

            // Keep it assigned to the current store
            messageTemplate.LimitedToStores = true;
            messageTemplate.Stores = [CurrentStoreId];

            if (!string.IsNullOrEmpty(prevAttachment) && prevAttachment != messageTemplate.AttachedDownloadId)
            {
                var attachment = await downloadService.GetDownloadById(prevAttachment);
                if (attachment != null)
                    await downloadService.DeleteDownload(attachment);
            }

            await messageTemplateService.UpdateMessageTemplate(messageTemplate);

            Success(translationService.GetResource("Admin.Content.MessageTemplates.Updated"));

            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = messageTemplate.Id });
            }

            return RedirectToAction("List");
        }

        model.HasAttachedDownload = !string.IsNullOrEmpty(model.AttachedDownloadId);
        model.AllowedTokens = messageTokenProvider.GetListOfAllowedTokens();
        foreach (var ea in await emailAccountService.GetAllEmailAccounts(CurrentStoreId))
            model.AvailableEmailAccounts.Add(ea.ToModel());

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var messageTemplate = await messageTemplateService.GetMessageTemplateById(id);
        if (messageTemplate == null)
            return RedirectToAction("List");

        if (!messageTemplate.LimitedToStores || !messageTemplate.Stores.Contains(CurrentStoreId))
            return RedirectToAction("List");

        await messageTemplateService.DeleteMessageTemplate(messageTemplate);

        if (!string.IsNullOrEmpty(messageTemplate.AttachedDownloadId))
        {
            var attachment = await downloadService.GetDownloadById(messageTemplate.AttachedDownloadId);
            if (attachment != null)
                await downloadService.DeleteDownload(attachment);
        }

        Success(translationService.GetResource("Admin.Content.MessageTemplates.Deleted"));
        return RedirectToAction("List");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CopyTemplate(MessageTemplateModel model)
    {
        var messageTemplate = await messageTemplateService.GetMessageTemplateById(model.Id);
        if (messageTemplate == null)
            return RedirectToAction("List");

        // Only allow copying global templates (LimitedToStores = false)
        if (messageTemplate.LimitedToStores)
            return RedirectToAction("List");

        // Prevent duplicate: check if a store-specific template with the same name already exists for the current store
        var existing = (await messageTemplateService.GetAllMessageTemplates("", keywords: messageTemplate.Name))
            .FirstOrDefault(t => t.Name == messageTemplate.Name && t.LimitedToStores && t.Stores.Contains(CurrentStoreId));
        if (existing != null)
        {
            Error(translationService.GetResource("Admin.Content.MessageTemplates.Fields.Name.AlreadyExists"));
            return RedirectToAction("List");
        }

        try
        {
            var newMessageTemplate = await messageTemplateService.CopyMessageTemplate(messageTemplate);
            // Assign copy to the current store
            newMessageTemplate.LimitedToStores = true;
            newMessageTemplate.Stores = [CurrentStoreId];
            await messageTemplateService.UpdateMessageTemplate(newMessageTemplate);

            Success(translationService.GetResource("Admin.Content.MessageTemplates.Copied"));
            return RedirectToAction("Edit", new { id = newMessageTemplate.Id });
        }
        catch (GrandException exc)
        {
            Error(exc.Message);
            return RedirectToAction("List");
        }
    }
}
