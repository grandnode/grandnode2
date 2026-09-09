using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Messages;
using Grand.Domain.Permissions;
using Grand.SharedKernel;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Messages;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.EmailAccounts)]
[AutoValidateAntiforgeryToken]
public abstract class BaseEmailAccountController(
    IEmailAccountViewModelService emailAccountViewModelService,
    IEmailAccountService emailAccountService,
    ITranslationService translationService,
    IAdminDataScope<EmailAccount> scope)
    : BaseController
{
    #region Create

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = await emailAccountViewModelService.PrepareEmailAccountModel();
        if (scope.DefaultStoreId is not null)
            model.StoreId = scope.DefaultStoreId;
        // Store never shows a Stores picker (its view emits only a hidden StoreId input) and must
        // never carry other stores' names/ids in a model handed to its widget zones - same
        // ShowStoreSelector gate ProductViewModelService uses for the same reason.
        if (!scope.ShowStoreSelector)
            model.AvailableStores.Clear();
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create(EmailAccountModel model, bool continueEditing)
    {
        // Force the account to belong to the current store (no-op for Admin, whose
        // scope.DefaultStoreId is null and so leaves whatever the caller posted untouched).
        if (scope.DefaultStoreId is not null)
            model.StoreId = scope.DefaultStoreId;

        if (ModelState.IsValid)
        {
            var emailAccount = await emailAccountViewModelService.InsertEmailAccountModel(model);
            Success(translationService.GetResource("Admin.Configuration.EmailAccounts.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = emailAccount.Id }) : RedirectToAction("List");
        }

        //If we got this far, something failed, redisplay form
        return View(model);
    }

    #endregion

    #region Edit

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var emailAccount = await emailAccountService.GetEmailAccountById(id);
        if (emailAccount == null || !await scope.HasAccess(emailAccount))
            //No email account found with the specified id, or not owned by the current store
            return RedirectToAction("List");

        var model = emailAccount.ToModel();
        await emailAccountViewModelService.PrepareAvailableStores(model);
        // See Create() above: Store must never receive other stores' names/ids on this model.
        if (!scope.ShowStoreSelector)
            model.AvailableStores.Clear();
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> Edit(EmailAccountModel model, bool continueEditing)
    {
        var emailAccount = await emailAccountService.GetEmailAccountById(model.Id);
        if (emailAccount == null || !await scope.HasAccess(emailAccount))
            //No email account found with the specified id, or not owned by the current store
            return RedirectToAction("List");

        // Prevent moving the account to another store (no-op for Admin).
        if (scope.DefaultStoreId is not null)
            model.StoreId = scope.DefaultStoreId;

        if (ModelState.IsValid)
        {
            emailAccount = await emailAccountViewModelService.UpdateEmailAccountModel(emailAccount, model);
            Success(translationService.GetResource("Admin.Configuration.EmailAccounts.Updated"));
            return continueEditing ? RedirectToAction("Edit", new { id = emailAccount.Id }) : RedirectToAction("List");
        }

        //If we got this far, something failed, redisplay form
        return View(model);
    }

    #endregion

    #region SendTestEmail

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> SendTestEmail(EmailAccountModel model)
    {
        var emailAccount = await emailAccountService.GetEmailAccountById(model.Id);
        if (emailAccount == null || !await scope.HasAccess(emailAccount))
            //No email account found with the specified id, or not owned by the current store
            return RedirectToAction("List");
        try
        {
            if (string.IsNullOrWhiteSpace(model.SendTestEmailTo))
                throw new GrandException("Enter test email address");
            if (ModelState.IsValid)
            {
                await emailAccountViewModelService.SendTestEmail(emailAccount, model);
                Success(translationService.GetResource("Admin.Configuration.EmailAccounts.SendTestEmail.Success"),
                    false);
            }
            else
            {
                Error(ModelState);
            }
        }
        catch (Exception exc)
        {
            Error(exc.Message);
        }

        //If we got this far, something failed, redisplay form
        return RedirectToAction("Edit", new { id = model.Id });
    }

    #endregion

    #region Delete

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> Delete(string id)
    {
        var emailAccount = await emailAccountService.GetEmailAccountById(id);
        if (emailAccount == null || !await scope.HasAccess(emailAccount))
            //No email account found with the specified id, or not owned by the current store
            return RedirectToAction("List");
        try
        {
            if (ModelState.IsValid)
            {
                await emailAccountService.DeleteEmailAccount(emailAccount);
                Success(translationService.GetResource("Admin.Configuration.EmailAccounts.Deleted"));
            }
            else
            {
                Error(ModelState);
            }

            return RedirectToAction("List");
        }
        catch (Exception exc)
        {
            // Deliberate widening from Store's original catch(GrandException) — see the design
            // spec's "Delete" section: unifies onto Admin's original, broader catch, turning a
            // previously-unhandled 500 on Store into a shown error message. Never hides a
            // currently-visible failure.
            Error(exc);
            return RedirectToAction("Edit", new { id = emailAccount.Id });
        }
    }

    #endregion
}
