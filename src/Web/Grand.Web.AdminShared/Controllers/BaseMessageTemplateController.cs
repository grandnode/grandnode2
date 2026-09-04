using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Messages;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Messages;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.MessageTemplates)]
[AutoValidateAntiforgeryToken]
public abstract class BaseMessageTemplateController(
    IMessageTemplateService messageTemplateService,
    IEmailAccountService emailAccountService,
    ILanguageService languageService,
    ITranslationService translationService,
    IMessageTokenProvider messageTokenProvider,
    IDownloadService downloadService,
    IAdminDataScope<MessageTemplate> scope)
    : BaseController
{
    public IActionResult Index() => RedirectToAction("List");

    #region Create

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = new MessageTemplateModel {
            AllowedTokens = messageTokenProvider.GetListOfAllowedTokens()
        };

        foreach (var ea in await emailAccountService.GetAllEmailAccounts(scope.DefaultStoreId ?? ""))
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
            // Store-only duplicate-name guard: a store manager may not create a second
            // store-exclusive template with the same name for their own store. Admin has no
            // such restriction (scope.DefaultStoreId is null there).
            if (!string.IsNullOrEmpty(scope.DefaultStoreId))
            {
                var existingStoreTemplate = (await messageTemplateService.GetAllMessageTemplates("", keywords: model.Name))
                    .FirstOrDefault(t =>
                        t.Name == model.Name &&
                        t.LimitedToStores &&
                        t.Stores.Contains(scope.DefaultStoreId));
                if (existingStoreTemplate != null)
                {
                    ModelState.AddModelError("Name", translationService.GetResource("Admin.Content.MessageTemplates.Fields.Name.AlreadyExists"));
                    model.HasAttachedDownload = !string.IsNullOrEmpty(model.AttachedDownloadId);
                    model.AllowedTokens = messageTokenProvider.GetListOfAllowedTokens();
                    foreach (var ea in await emailAccountService.GetAllEmailAccounts(scope.DefaultStoreId ?? ""))
                        model.AvailableEmailAccounts.Add(ea.ToModel());
                    return View(model);
                }
            }

            var messageTemplate = model.ToEntity();
            if (!model.HasAttachedDownload)
                messageTemplate.AttachedDownloadId = "";
            if (model.SendImmediately)
                messageTemplate.DelayBeforeSend = null;

            if (!string.IsNullOrEmpty(scope.DefaultStoreId))
            {
                messageTemplate.LimitedToStores = true;
                messageTemplate.Stores = [scope.DefaultStoreId];
            }

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
        foreach (var ea in await emailAccountService.GetAllEmailAccounts(scope.DefaultStoreId ?? ""))
            model.AvailableEmailAccounts.Add(ea.ToModel());

        return View(model);
    }

    #endregion

    #region Edit

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var messageTemplate = await messageTemplateService.GetMessageTemplateById(id);
        if (messageTemplate == null)
            return RedirectToAction("List");

        if (!await scope.CanView(messageTemplate))
            return RedirectToAction("List");

        var model = messageTemplate.ToModel();
        model.IsReadOnly = !await scope.HasAccess(messageTemplate);
        model.CanCopy = string.IsNullOrEmpty(scope.DefaultStoreId) || !await scope.HasAccess(messageTemplate);
        model.SendImmediately = !model.DelayBeforeSend.HasValue;
        model.HasAttachedDownload = !string.IsNullOrEmpty(model.AttachedDownloadId);
        model.AllowedTokens = messageTokenProvider.GetListOfAllowedTokens();

        foreach (var ea in await emailAccountService.GetAllEmailAccounts(scope.DefaultStoreId ?? ""))
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

        if (!await scope.HasAccess(messageTemplate))
            return RedirectToAction("List");

        var prevAttachment = messageTemplate.AttachedDownloadId;

        if (ModelState.IsValid)
        {
            messageTemplate = model.ToEntity(messageTemplate);
            if (!model.HasAttachedDownload)
                messageTemplate.AttachedDownloadId = "";
            if (model.SendImmediately)
                messageTemplate.DelayBeforeSend = null;

            if (!string.IsNullOrEmpty(scope.DefaultStoreId))
            {
                messageTemplate.LimitedToStores = true;
                messageTemplate.Stores = [scope.DefaultStoreId];
            }

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
        foreach (var ea in await emailAccountService.GetAllEmailAccounts(scope.DefaultStoreId ?? ""))
            model.AvailableEmailAccounts.Add(ea.ToModel());

        return View(model);
    }

    #endregion

    #region Delete

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var messageTemplate = await messageTemplateService.GetMessageTemplateById(id);
        if (messageTemplate == null)
            return RedirectToAction("List");

        if (!await scope.HasAccess(messageTemplate))
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

    #endregion

    #region Copy

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CopyTemplate(MessageTemplateModel model)
    {
        var messageTemplate = await messageTemplateService.GetMessageTemplateById(model.Id);
        if (messageTemplate == null)
            return RedirectToAction("List");

        // Store-only guard: only a fully global template (LimitedToStores == false) may be
        // copied — matches Store's original behavior exactly (it never allowed copying any
        // store-limited template, regardless of which store owned it). Admin
        // (scope.DefaultStoreId == null) is unrestricted, preserving its original
        // unlimited-copy behavior.
        //
        // NOTE: an earlier version of this guard used `scope.HasAccess(messageTemplate)` —
        // "deny only if the caller already exclusively owns it" — which is NOT equivalent:
        // it let a store copy another store's exclusive template (HasAccess is false there
        // too), leaking that template's Name/Subject/Body into the caller's own store. Found
        // live: store1 successfully copied store2's exclusive "Customer.PasswordRecovery"
        // template via a crafted CopyTemplate POST. Fixed by checking LimitedToStores
        // directly, not ownership of it.
        if (!string.IsNullOrEmpty(scope.DefaultStoreId))
        {
            if (messageTemplate.LimitedToStores)
                return RedirectToAction("List");

            var existing = (await messageTemplateService.GetAllMessageTemplates("", keywords: messageTemplate.Name))
                .FirstOrDefault(t => t.Name == messageTemplate.Name && t.LimitedToStores && t.Stores.Contains(scope.DefaultStoreId));
            if (existing != null)
            {
                Error(translationService.GetResource("Admin.Content.MessageTemplates.Fields.Name.AlreadyExists"));
                return RedirectToAction("List");
            }
        }

        try
        {
            var newMessageTemplate = await messageTemplateService.CopyMessageTemplate(messageTemplate);

            if (!string.IsNullOrEmpty(scope.DefaultStoreId))
            {
                newMessageTemplate.LimitedToStores = true;
                newMessageTemplate.Stores = [scope.DefaultStoreId];
                await messageTemplateService.UpdateMessageTemplate(newMessageTemplate);
            }

            Success(translationService.GetResource("Admin.Content.MessageTemplates.Copied"));
            return RedirectToAction("Edit", new { id = newMessageTemplate.Id });
        }
        catch (Exception exc)
        {
            Error(exc.Message);
            return string.IsNullOrEmpty(scope.DefaultStoreId)
                ? RedirectToAction("Edit", new { id = model.Id })
                : RedirectToAction("List");
        }
    }

    #endregion
}
