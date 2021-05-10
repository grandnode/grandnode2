using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Marketing.Interfaces.Campaigns;
using Grand.Business.Marketing.Interfaces.Customers;
using Grand.Business.Marketing.Interfaces.Newsletters;
using Grand.Business.Messages.Interfaces;
using Grand.Domain.Messages;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Messages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Services
{
    public partial class CampaignViewModelService : ICampaignViewModelService
    {

        private readonly ICampaignService _campaignService;
        private readonly ICustomerService _customerService;
        private readonly IGroupService _groupService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IMessageTokenProvider _messageTokenProvider;
        private readonly IStoreService _storeService;
        private readonly ILanguageService _languageService;
        private readonly ICustomerTagService _customerTagService;
        private readonly INewsletterCategoryService _newsletterCategoryService;

        public CampaignViewModelService(ICampaignService campaignService,
            ICustomerService customerService,
            IGroupService groupService,
            IDateTimeService dateTimeService,
            IEmailAccountService emailAccountService,
            IMessageTokenProvider messageTokenProvider,
            IStoreService storeService,
            ILanguageService languageService,
            ICustomerTagService customerTagService,
            INewsletterCategoryService newsletterCategoryService)
        {
            _campaignService = campaignService;
            _customerService = customerService;
            _groupService = groupService;
            _dateTimeService = dateTimeService;
            _emailAccountService = emailAccountService;
            _messageTokenProvider = messageTokenProvider;
            _storeService = storeService;
            _languageService = languageService;
            _customerTagService = customerTagService;
            _newsletterCategoryService = newsletterCategoryService;
        }

        protected virtual string FormatTokens(string[] tokens)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                sb.Append(token);
                if (i != tokens.Length - 1)
                    sb.Append(", ");
            }

            return sb.ToString();
        }

        protected virtual async Task PrepareStoresModel(CampaignModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var stores = await _storeService.GetAllStores();
            foreach (var store in stores)
            {
                model.AvailableStores.Add(new SelectListItem
                {
                    Text = store.Shortcut,
                    Value = store.Id.ToString()
                });
            }
        }

        protected virtual async Task PrepareLanguagesModel(CampaignModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var languages = await _languageService.GetAllLanguages();
            foreach (var lang in languages)
            {
                model.AvailableLanguages.Add(new SelectListItem
                {
                    Text = lang.Name,
                    Value = lang.Id.ToString()
                });
            }
        }

        protected virtual async Task PrepareCustomerTagsModel(CampaignModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            model.AvailableCustomerTags = (await _customerTagService.GetAllCustomerTags()).Select(ct => new SelectListItem() { Text = ct.Name, Value = ct.Id, Selected = model.CustomerTags.Contains(ct.Id) }).ToList();
            model.CustomerTags = model.CustomerTags == null ? new List<string>() : model.CustomerTags;
        }

        protected virtual async Task PrepareCustomerGroupsModel(CampaignModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            model.AvailableCustomerGroups = (await _groupService.GetAllCustomerGroups()).Select(ct => new SelectListItem() { Text = ct.Name, Value = ct.Id, Selected = model.CustomerGroups.Contains(ct.Id) }).ToList();
            model.CustomerGroups = model.CustomerGroups == null ? new List<string>() : model.CustomerGroups;
        }

        protected virtual async Task PrepareNewsletterCategoriesModel(CampaignModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            model.AvailableNewsletterCategories = (await _newsletterCategoryService.GetAllNewsletterCategory()).Select(ct => new SelectListItem() { Text = ct.Name, Value = ct.Id, Selected = model.NewsletterCategories.Contains(ct.Id) }).ToList();
            model.NewsletterCategories = model.NewsletterCategories == null ? new List<string>() : model.NewsletterCategories;
        }

        protected virtual async Task PrepareEmailAccounts(CampaignModel model)
        {
            //available email accounts
            foreach (var ea in await _emailAccountService.GetAllEmailAccounts())
                model.AvailableEmailAccounts.Add(ea.ToModel());
        }

        public virtual async Task<CampaignModel> PrepareCampaignModel()
        {
            var model = new CampaignModel();
            model.AllowedTokens = _messageTokenProvider.GetListOfCampaignAllowedTokens();
            //stores
            await PrepareStoresModel(model);
            //languages
            await PrepareLanguagesModel(model);
            //Tags
            await PrepareCustomerTagsModel(model);
            //Groups
            await PrepareCustomerGroupsModel(model);
            //Newsletter categories
            await PrepareNewsletterCategoriesModel(model);
            //email
            await PrepareEmailAccounts(model);
            return model;
        }

        public virtual async Task<CampaignModel> PrepareCampaignModel(CampaignModel model)
        {
            //If we got this far, something failed, redisplay form
            model.AllowedTokens = _messageTokenProvider.GetListOfCampaignAllowedTokens();
            //stores
            await PrepareStoresModel(model);
            //languages
            await PrepareLanguagesModel(model);
            //Tags
            await PrepareCustomerTagsModel(model);
            //Newsletter categories
            await PrepareNewsletterCategoriesModel(model);
            //Groups
            await PrepareCustomerGroupsModel(model);
            //email
            await PrepareEmailAccounts(model);
            return model;
        }

        public virtual async Task<CampaignModel> PrepareCampaignModel(Campaign campaign)
        {
            var model = campaign.ToModel();
            model.AllowedTokens = _messageTokenProvider.GetListOfCampaignAllowedTokens();
            //stores
            await PrepareStoresModel(model);
            //languages
            await PrepareLanguagesModel(model);
            //Tags
            await PrepareCustomerTagsModel(model);
            //Newsletter categories
            await PrepareNewsletterCategoriesModel(model);
            //Groups
            await PrepareCustomerGroupsModel(model);
            //email
            await PrepareEmailAccounts(model);

            return model;
        }

        public virtual async Task<Campaign> InsertCampaignModel(CampaignModel model)
        {
            var campaign = model.ToEntity();
            campaign.CreatedOnUtc = DateTime.UtcNow;
            await _campaignService.InsertCampaign(campaign);
            return campaign;
        }
        public virtual async Task<Campaign> UpdateCampaignModel(Campaign campaign, CampaignModel model)
        {
            campaign = model.ToEntity(campaign);
            campaign.CustomerGroups.Clear();
            foreach (var item in model.CustomerGroups)
            {
                campaign.CustomerGroups.Add(item);
            }
            campaign.CustomerTags.Clear();
            foreach (var item in model.CustomerTags)
            {
                campaign.CustomerTags.Add(item);
            }
            campaign.NewsletterCategories.Clear();
            foreach (var item in model.NewsletterCategories)
            {
                campaign.NewsletterCategories.Add(item);
            }
            await _campaignService.UpdateCampaign(campaign);
            return campaign;
        }
        public virtual async Task<(IEnumerable<CampaignModel> campaignModels, int totalCount)> PrepareCampaignModels()
        {
            var campaigns = await _campaignService.GetAllCampaigns();
            return (campaigns.Select(x =>
            {
                var model = x.ToModel();
                model.CreatedOn = _dateTimeService.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);
                return model;
            }), campaigns.Count);
        }
    }
}
