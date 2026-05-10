using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Messages;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.SharedKernel;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Messages;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.EmailAccounts)]
public class EmailAccountController(
    IEmailAccountService emailAccountService,
    IEmailAccountViewModelService emailAccountViewModelService,
    ITranslationService translationService,
    IContextAccessor contextAccessor) : BaseStoreController
{
    private string CurrentStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    public IActionResult List()
    {
        return View();
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> List(DataSourceRequest command)
    {
        var emailAccounts = await emailAccountService.GetAllEmailAccounts(CurrentStoreId, pageIndex: command.Page - 1, pageSize: command.PageSize);
        var emailAccountModels = emailAccounts.Select(x => x.ToModel()).ToList();

        var gridModel = new DataSourceResult {
            Data = emailAccountModels,
            Total = emailAccounts.TotalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public IActionResult Create()
    {
        var model = new EmailAccountModel {
            Port = 25,
            StoreId = CurrentStoreId
        };
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create(EmailAccountModel model, bool continueEditing)
    {
        // Force the account to belong to the current store
        model.StoreId = CurrentStoreId;

        if (ModelState.IsValid)
        {
            var emailAccount = await emailAccountViewModelService.InsertEmailAccountModel(model);
            Success(translationService.GetResource("Admin.Configuration.EmailAccounts.Added"));
            return continueEditing
                ? RedirectToAction("Edit", new { id = emailAccount.Id })
                : RedirectToAction("List");
        }

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var emailAccount = await emailAccountService.GetEmailAccountById(id);
        if (emailAccount == null || emailAccount.StoreId != CurrentStoreId)
            return RedirectToAction("List");

        var model = emailAccount.ToModel();
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> Edit(EmailAccountModel model, bool continueEditing)
    {
        var emailAccount = await emailAccountService.GetEmailAccountById(model.Id);
        if (emailAccount == null || emailAccount.StoreId != CurrentStoreId)
            return RedirectToAction("List");

        // Prevent moving the account to another store
        model.StoreId = CurrentStoreId;

        if (ModelState.IsValid)
        {
            emailAccount = await emailAccountViewModelService.UpdateEmailAccountModel(emailAccount, model);
            Success(translationService.GetResource("Admin.Configuration.EmailAccounts.Updated"));
            return continueEditing
                ? RedirectToAction("Edit", new { id = emailAccount.Id })
                : RedirectToAction("List");
        }

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> SendTestEmail(EmailAccountModel model)
    {
        var emailAccount = await emailAccountService.GetEmailAccountById(model.Id);
        if (emailAccount == null || emailAccount.StoreId != CurrentStoreId)
            return RedirectToAction("List");

        try
        {
            if (string.IsNullOrWhiteSpace(model.SendTestEmailTo))
                throw new GrandException(translationService.GetResource("Admin.Configuration.EmailAccounts.EnterTestEmail"));
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

        return RedirectToAction("Edit", new { id = model.Id });
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> Delete(string id)
    {
        var emailAccount = await emailAccountService.GetEmailAccountById(id);
        if (emailAccount == null || emailAccount.StoreId != CurrentStoreId)
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
        catch (GrandException exc)
        {
            Error(exc);
            return RedirectToAction("Edit", new { id = emailAccount.Id });
        }
    }
}
