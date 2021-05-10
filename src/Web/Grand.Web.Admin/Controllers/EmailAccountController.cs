using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Services.Security;
using Grand.Business.Messages.Interfaces;
using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.SharedKernel;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Messages;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.EmailAccounts)]
    public partial class EmailAccountController : BaseAdminController
    {
        private readonly IEmailAccountViewModelService _emailAccountViewModelService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly ITranslationService _translationService;
        private readonly ISettingService _settingService;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly ICacheBase _cacheBase;

        public EmailAccountController(IEmailAccountViewModelService emailAccountViewModelService, IEmailAccountService emailAccountService,
            ITranslationService translationService, ISettingService settingService,
            EmailAccountSettings emailAccountSettings, ICacheBase cacheBase)
        {
            _emailAccountViewModelService = emailAccountViewModelService;
            _emailAccountService = emailAccountService;
            _translationService = translationService;
            _emailAccountSettings = emailAccountSettings;
            _settingService = settingService;
            _cacheBase = cacheBase;
        }

        public IActionResult List() => View();

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var emailAccountModels = (await _emailAccountService.GetAllEmailAccounts())
                                    .Select(x => x.ToModel())
                                    .ToList();
            foreach (var eam in emailAccountModels)
                eam.IsDefaultEmailAccount = eam.Id == _emailAccountSettings.DefaultEmailAccountId;

            var gridModel = new DataSourceResult
            {
                Data = emailAccountModels,
                Total = emailAccountModels.Count()
            };

            return Json(gridModel);
        }
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> MarkAsDefaultEmail(string id)
        {
            var defaultEmailAccount = await _emailAccountService.GetEmailAccountById(id);
            if (defaultEmailAccount != null)
            {
                _emailAccountSettings.DefaultEmailAccountId = defaultEmailAccount.Id;
                await _settingService.SaveSetting(_emailAccountSettings);
            }

            //now clear cache
            await _cacheBase.Clear();

            return RedirectToAction("List");
        }
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public IActionResult Create()
        {
            var model = _emailAccountViewModelService.PrepareEmailAccountModel();
            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create(EmailAccountModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var emailAccount = await _emailAccountViewModelService.InsertEmailAccountModel(model);
                Success(_translationService.GetResource("Admin.Configuration.EmailAccounts.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = emailAccount.Id }) : RedirectToAction("List");
            }
            //If we got this far, something failed, redisplay form
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var emailAccount = await _emailAccountService.GetEmailAccountById(id);
            if (emailAccount == null)
                //No email account found with the specified id
                return RedirectToAction("List");

            return View(emailAccount.ToModel());
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> Edit(EmailAccountModel model, bool continueEditing)
        {
            var emailAccount = await _emailAccountService.GetEmailAccountById(model.Id);
            if (emailAccount == null)
                //No email account found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                emailAccount = await _emailAccountViewModelService.UpdateEmailAccountModel(emailAccount, model);
                Success(_translationService.GetResource("Admin.Configuration.EmailAccounts.Updated"));
                return continueEditing ? RedirectToAction("Edit", new { id = emailAccount.Id }) : RedirectToAction("List");
            }
            //If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> SendTestEmail(EmailAccountModel model)
        {
            var emailAccount = await _emailAccountService.GetEmailAccountById(model.Id);
            if (emailAccount == null)
                //No email account found with the specified id
                return RedirectToAction("List");
            try
            {
                if (String.IsNullOrWhiteSpace(model.SendTestEmailTo))
                    throw new GrandException("Enter test email address");
                if (ModelState.IsValid)
                {
                    await _emailAccountViewModelService.SendTestEmail(emailAccount, model);
                    Success(_translationService.GetResource("Admin.Configuration.EmailAccounts.SendTestEmail.Success"), false);
                }
                else
                    Error(ModelState);
            }
            catch (Exception exc)
            {
                Error(exc.Message, false);
            }

            //If we got this far, something failed, redisplay form
            return RedirectToAction("Edit", new { id = model.Id });
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        public async Task<IActionResult> Delete(string id)
        {
            var emailAccount = await _emailAccountService.GetEmailAccountById(id);
            if (emailAccount == null)
                //No email account found with the specified id
                return RedirectToAction("List");
            try
            {
                if (ModelState.IsValid)
                {
                    await _emailAccountService.DeleteEmailAccount(emailAccount);
                    Success(_translationService.GetResource("Admin.Configuration.EmailAccounts.Deleted"));
                }
                else
                    Error(ModelState);

                return RedirectToAction("List");
            }
            catch (Exception exc)
            {
                Error(exc);
                return RedirectToAction("Edit", new { id = emailAccount.Id });
            }
        }
    }
}
