using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Business.Marketing.Interfaces.Campaigns;
using Grand.Business.Marketing.Interfaces.Newsletters;
using Grand.Business.Messages.Interfaces;
using Grand.Business.System.Interfaces.ExportImport;
using Grand.Domain.Messages;
using Grand.Infrastructure;
using Grand.SharedKernel;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Messages;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Campaigns)]
    public partial class CampaignController : BaseAdminController
    {
        private readonly ICampaignService _campaignService;
        private readonly ICampaignViewModelService _campaignViewModelService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly IExportManager _exportManager;
        private readonly EmailAccountSettings _emailAccountSettings;

        public CampaignController(ICampaignService campaignService, ICampaignViewModelService campaignViewModelService,
            IEmailAccountService emailAccountService,
            INewsLetterSubscriptionService newsLetterSubscriptionService,
            ITranslationService translationService,
            IWorkContext workContext,
            IStoreService storeService,
            IExportManager exportManager,
            EmailAccountSettings emailAccountSettings)
        {
            _campaignService = campaignService;
            _campaignViewModelService = campaignViewModelService;
            _emailAccountService = emailAccountService;
            _emailAccountSettings = emailAccountSettings;
            _newsLetterSubscriptionService = newsLetterSubscriptionService;
            _translationService = translationService;
            _workContext = workContext;
            _storeService = storeService;
            _exportManager = exportManager;
        }

        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List() => View();

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var model = await _campaignViewModelService.PrepareCampaignModels();
            var gridModel = new DataSourceResult
            {
                Data = model.campaignModels,
                Total = model.totalCount
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> Customers(string campaignId, DataSourceRequest command)
        {
            var campaign = await _campaignService.GetCampaignById(campaignId);
            var customers = await _campaignService.CustomerSubscriptions(campaign, command.Page - 1, command.PageSize);

            var gridModel = new DataSourceResult
            {
                Data = customers,
                Total = customers.TotalCount
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> History(string campaignId, DataSourceRequest command)
        {
            var campaign = await _campaignService.GetCampaignById(campaignId);
            var history = await _campaignService.GetCampaignHistory(campaign, command.Page - 1, command.PageSize);

            var gridModel = new DataSourceResult
            {
                Data = history.Select(x => new
                {
                    Email = x.Email,
                    SentDate = x.CreatedDateUtc,
                }),
                Total = history.TotalCount
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Export)]
        public async Task<IActionResult> ExportCsv(string campaignId)
        {
            try
            {
                var campaign = await _campaignService.GetCampaignById(campaignId);
                var customers = await _campaignService.CustomerSubscriptions(campaign);
                string result = _exportManager.ExportNewsletterSubscribersToTxt(customers.Select(x => x.Email).ToList());

                string fileName = String.Format("newsletter_emails_campaign_{0}_{1}.txt", campaign.Name, CommonHelper.GenerateRandomDigitCode(4));
                return File(Encoding.UTF8.GetBytes(result), "text/csv", fileName);
            }
            catch (Exception exc)
            {
                Error(exc);
                return RedirectToAction("List");
            }
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create()
        {
            var model = await _campaignViewModelService.PrepareCampaignModel();
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Create(CampaignModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var campaign = await _campaignViewModelService.InsertCampaignModel(model);

                Success(_translationService.GetResource("admin.marketing.Campaigns.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = campaign.Id }) : RedirectToAction("List");
            }

            model = await _campaignViewModelService.PrepareCampaignModel(model);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var campaign = await _campaignService.GetCampaignById(id);
            if (campaign == null)
                //No campaign found with the specified id
                return RedirectToAction("List");

            var model = await _campaignViewModelService.PrepareCampaignModel(campaign);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Edit(CampaignModel model, bool continueEditing)
        {
            var campaign = await _campaignService.GetCampaignById(model.Id);
            if (campaign == null)
                //No campaign found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                campaign = await _campaignViewModelService.UpdateCampaignModel(campaign, model);
                Success(_translationService.GetResource("admin.marketing.Campaigns.Updated"));
                //selected tab
                await SaveSelectedTabIndex();

                return continueEditing ? RedirectToAction("Edit", new { id = campaign.Id }) : RedirectToAction("List");
            }
            model = await _campaignViewModelService.PrepareCampaignModel(model);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> SendTestEmail(CampaignModel model)
        {
            var campaign = await _campaignService.GetCampaignById(model.Id);
            if (campaign == null)
                //No campaign found with the specified id
                return RedirectToAction("List");

            model = await _campaignViewModelService.PrepareCampaignModel(model);
            try
            {
                var emailAccount = await _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId);
                if (emailAccount == null)
                    throw new GrandException("Email account could not be loaded");

                var subscription = await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreId(model.TestEmail, _workContext.CurrentStore.Id);
                if (subscription != null)
                {
                    //there's a subscription. use it
                    var subscriptions = new List<NewsLetterSubscription>
                    {
                        subscription
                    };
                    await _campaignService.SendCampaign(campaign, emailAccount, subscriptions);
                }
                else
                {
                    //no subscription found
                    await _campaignService.SendCampaign(campaign, emailAccount, model.TestEmail);
                }

                Success(_translationService.GetResource("admin.marketing.Campaigns.TestEmailSentToCustomers"), false);
                return RedirectToAction("Edit", new { id = campaign.Id });
            }
            catch (Exception exc)
            {
                Error(exc, false);
            }

            //If we got this far, something failed, redisplay form
            return RedirectToAction("Edit", new { id = campaign.Id });
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> SendMassEmail(CampaignModel model)
        {
            var campaign = await _campaignService.GetCampaignById(model.Id);
            if (campaign == null)
                //No campaign found with the specified id
                return RedirectToAction("List");

            model = await _campaignViewModelService.PrepareCampaignModel(model);
            model.CustomerTags = campaign.CustomerTags.ToList();
            model.CustomerGroups = campaign.CustomerGroups.ToList();
            try
            {
                var emailAccount = await _emailAccountService.GetEmailAccountById(campaign.EmailAccountId);
                if (emailAccount == null)
                    throw new GrandException("Email account could not be loaded");

                //subscribers of certain store?
                var store = await _storeService.GetStoreById(campaign.StoreId);
                var storeId = store != null ? store.Id : "";
                var subscriptions = await _campaignService.CustomerSubscriptions(campaign);
                var totalEmailsSent = await _campaignService.SendCampaign(campaign, emailAccount, subscriptions);
                Success(string.Format(_translationService.GetResource("admin.marketing.Campaigns.MassEmailSentToCustomers"), totalEmailsSent), false);
                return RedirectToAction("Edit", new { id = campaign.Id });
            }
            catch (Exception exc)
            {
                Error(exc, false);
            }

            //If we got this far, something failed, redisplay form
            return RedirectToAction("Edit", new { id = campaign.Id });
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var campaign = await _campaignService.GetCampaignById(id);
            if (campaign == null)
                //No campaign found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                await _campaignService.DeleteCampaign(campaign);
                Success(_translationService.GetResource("admin.marketing.Campaigns.Deleted"));
                return RedirectToAction("List");
            }
            Error(ModelState);
            return RedirectToAction("Edit", new { id = id });
        }
    }
}
