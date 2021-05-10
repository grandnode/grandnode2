using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Business.Messages.Interfaces;
using Grand.Domain.Messages;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Messages;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.MessageTemplates)]
    public partial class MessageTemplateController : BaseAdminController
    {
        #region Fields

        private readonly IMessageTemplateService _messageTemplateService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly ILanguageService _languageService;
        private readonly ITranslationService _translationService;
        private readonly IMessageTokenProvider _messageTokenProvider;
        private readonly IStoreService _storeService;
        private readonly EmailAccountSettings _emailAccountSettings;

        #endregion Fields

        #region Constructors

        public MessageTemplateController(IMessageTemplateService messageTemplateService,
            IEmailAccountService emailAccountService,
            ILanguageService languageService,
            ITranslationService translationService,
            IMessageTokenProvider messageTokenProvider,
            IStoreService storeService,
            EmailAccountSettings emailAccountSettings)
        {
            _messageTemplateService = messageTemplateService;
            _emailAccountService = emailAccountService;
            _languageService = languageService;
            _translationService = translationService;
            _messageTokenProvider = messageTokenProvider;
            _storeService = storeService;
            _emailAccountSettings = emailAccountSettings;
        }

        #endregion

        #region Methods

        public IActionResult Index() => RedirectToAction("List");

        public async Task<IActionResult> List()
        {
            var model = new MessageTemplateListModel();
            //stores
            model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
            foreach (var s in await _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id.ToString() });

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command, MessageTemplateListModel model)
        {
            var messageTemplates = await _messageTemplateService.GetAllMessageTemplates(model.SearchStoreId);

            if (!string.IsNullOrEmpty(model.Name))
            {
                messageTemplates = messageTemplates.Where
                    (x => x.Name.ToLowerInvariant().Contains(model.Name.ToLowerInvariant()) ||
                    x.Subject.ToLowerInvariant().Contains(model.Name.ToLowerInvariant())).ToList();
            }
            var items = new List<MessageTemplateModel>();
            foreach (var x in messageTemplates)
            {
                var templateModel = x.ToModel();
                var stores = (await _storeService
                        .GetAllStores())
                        .Where(s => !x.LimitedToStores || templateModel.Stores.Contains(s.Id))
                        .ToList();
                for (int i = 0; i < stores.Count; i++)
                {
                    templateModel.ListOfStores += stores[i].Shortcut;
                    if (i != stores.Count - 1)
                        templateModel.ListOfStores += ", ";
                }
                items.Add(templateModel);
            }
            var gridModel = new DataSourceResult
            {
                Data = items,
                Total = messageTemplates.Count
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new MessageTemplateModel();

            //Stores
            model.AllowedTokens = _messageTokenProvider.GetListOfAllowedTokens();
            //available email accounts
            foreach (var ea in await _emailAccountService.GetAllEmailAccounts())
                model.AvailableEmailAccounts.Add(ea.ToModel());

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Create(MessageTemplateModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var messageTemplate = model.ToEntity();
                //attached file
                if (!model.HasAttachedDownload)
                    messageTemplate.AttachedDownloadId = "";
                if (model.SendImmediately)
                    messageTemplate.DelayBeforeSend = null;

                await _messageTemplateService.InsertMessageTemplate(messageTemplate);

                Success(_translationService.GetResource("Admin.Content.MessageTemplates.AddNew"));

                if (continueEditing)
                {
                    //selected tab
                    await SaveSelectedTabIndex();

                    return RedirectToAction("Edit", new { id = messageTemplate.Id });
                }
                return RedirectToAction("List");
            }


            //If we got this far, something failed, redisplay form
            model.HasAttachedDownload = !String.IsNullOrEmpty(model.AttachedDownloadId);
            model.AllowedTokens = _messageTokenProvider.GetListOfAllowedTokens();
            //available email accounts
            foreach (var ea in await _emailAccountService.GetAllEmailAccounts())
                model.AvailableEmailAccounts.Add(ea.ToModel());
            //Store
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var messageTemplate = await _messageTemplateService.GetMessageTemplateById(id);
            if (messageTemplate == null)
                //No message template found with the specified id
                return RedirectToAction("List");

            var model = messageTemplate.ToModel();
            model.SendImmediately = !model.DelayBeforeSend.HasValue;
            model.HasAttachedDownload = !String.IsNullOrEmpty(model.AttachedDownloadId);
            model.AllowedTokens = _messageTokenProvider.GetListOfAllowedTokens();
            //available email accounts
            foreach (var ea in await _emailAccountService.GetAllEmailAccounts())
                model.AvailableEmailAccounts.Add(ea.ToModel());

            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.BccEmailAddresses = messageTemplate.GetTranslation(x => x.BccEmailAddresses, languageId, false);
                locale.Subject = messageTemplate.GetTranslation(x => x.Subject, languageId, false);
                locale.Body = messageTemplate.GetTranslation(x => x.Body, languageId, false);

                var emailAccountId = messageTemplate.GetTranslation(x => x.EmailAccountId, languageId, false);
                locale.EmailAccountId = !String.IsNullOrEmpty(emailAccountId) ? emailAccountId : _emailAccountSettings.DefaultEmailAccountId;
            });

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Edit(MessageTemplateModel model, bool continueEditing)
        {
            var messageTemplate = await _messageTemplateService.GetMessageTemplateById(model.Id);
            if (messageTemplate == null)
                //No message template found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                messageTemplate = model.ToEntity(messageTemplate);
                //attached file
                if (!model.HasAttachedDownload)
                    messageTemplate.AttachedDownloadId = "";
                if (model.SendImmediately)
                    messageTemplate.DelayBeforeSend = null;

                await _messageTemplateService.UpdateMessageTemplate(messageTemplate);

                Success(_translationService.GetResource("Admin.Content.MessageTemplates.Updated"));

                if (continueEditing)
                {
                    //selected tab
                    await SaveSelectedTabIndex();

                    return RedirectToAction("Edit", new { id = messageTemplate.Id });
                }
                return RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            model.HasAttachedDownload = !String.IsNullOrEmpty(model.AttachedDownloadId);
            model.AllowedTokens = _messageTokenProvider.GetListOfAllowedTokens();
            //available email accounts
            foreach (var ea in await _emailAccountService.GetAllEmailAccounts())
                model.AvailableEmailAccounts.Add(ea.ToModel());

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var messageTemplate = await _messageTemplateService.GetMessageTemplateById(id);
            if (messageTemplate == null)
                //No message template found with the specified id
                return RedirectToAction("List");

            await _messageTemplateService.DeleteMessageTemplate(messageTemplate);

            Success(_translationService.GetResource("Admin.Content.MessageTemplates.Deleted"));
            return RedirectToAction("List");
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> CopyTemplate(MessageTemplateModel model)
        {
            var messageTemplate = await _messageTemplateService.GetMessageTemplateById(model.Id);
            if (messageTemplate == null)
                //No message template found with the specified id
                return RedirectToAction("List");

            try
            {
                var newMessageTemplate = await _messageTemplateService.CopyMessageTemplate(messageTemplate);
                Success("The message template has been copied successfully");
                return RedirectToAction("Edit", new { id = newMessageTemplate.Id });
            }
            catch (Exception exc)
            {
                Error(exc.Message);
                return RedirectToAction("Edit", new { id = model.Id });
            }
        }

        #endregion
    }
}