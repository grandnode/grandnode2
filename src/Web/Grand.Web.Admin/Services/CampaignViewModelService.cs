using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Marketing.Campaigns;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Business.Core.Interfaces.Marketing.Newsletters;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Messages;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Messages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

public class CampaignViewModelService : ICampaignViewModelService
{
    private readonly ICampaignService _campaignService;
    private readonly ICustomerTagService _customerTagService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IEmailAccountService _emailAccountService;
    private readonly IGroupService _groupService;
    private readonly ILanguageService _languageService;
    private readonly IMessageTokenProvider _messageTokenProvider;
    private readonly INewsletterCategoryService _newsletterCategoryService;
    private readonly IStoreService _storeService;

    public CampaignViewModelService(ICampaignService campaignService,
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
        _groupService = groupService;
        _dateTimeService = dateTimeService;
        _emailAccountService = emailAccountService;
        _messageTokenProvider = messageTokenProvider;
        _storeService = storeService;
        _languageService = languageService;
        _customerTagService = customerTagService;
        _newsletterCategoryService = newsletterCategoryService;
    }

    public virtual async Task<CampaignModel> PrepareCampaignModel()
    {
        var model = new CampaignModel {
            AllowedTokens = _messageTokenProvider.GetListOfCampaignAllowedTokens()
        };
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
        await _campaignService.InsertCampaign(campaign);
        return campaign;
    }

    public virtual async Task<Campaign> UpdateCampaignModel(Campaign campaign, CampaignModel model)
    {
        campaign = model.ToEntity(campaign);
        campaign.CustomerGroups.Clear();
        foreach (var item in model.CustomerGroups) campaign.CustomerGroups.Add(item);
        campaign.CustomerTags.Clear();
        foreach (var item in model.CustomerTags) campaign.CustomerTags.Add(item);
        campaign.NewsletterCategories.Clear();
        foreach (var item in model.NewsletterCategories) campaign.NewsletterCategories.Add(item);
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

    protected virtual async Task PrepareStoresModel(CampaignModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var stores = await _storeService.GetAllStores();
        foreach (var store in stores)
            model.AvailableStores.Add(new SelectListItem {
                Text = store.Shortcut,
                Value = store.Id
            });
    }

    protected virtual async Task PrepareLanguagesModel(CampaignModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var languages = await _languageService.GetAllLanguages();
        foreach (var lang in languages)
            model.AvailableLanguages.Add(new SelectListItem {
                Text = lang.Name,
                Value = lang.Id
            });
    }

    protected virtual async Task PrepareCustomerTagsModel(CampaignModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        model.AvailableCustomerTags = (await _customerTagService.GetAllCustomerTags()).Select(ct => new SelectListItem
            { Text = ct.Name, Value = ct.Id, Selected = model.CustomerTags.Contains(ct.Id) }).ToList();
        model.CustomerTags ??= new List<string>();
    }

    protected virtual async Task PrepareCustomerGroupsModel(CampaignModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        model.AvailableCustomerGroups = (await _groupService.GetAllCustomerGroups()).Select(ct => new SelectListItem
            { Text = ct.Name, Value = ct.Id, Selected = model.CustomerGroups.Contains(ct.Id) }).ToList();
        model.CustomerGroups ??= new List<string>();
    }

    protected virtual async Task PrepareNewsletterCategoriesModel(CampaignModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        model.AvailableNewsletterCategories = (await _newsletterCategoryService.GetAllNewsletterCategory()).Select(ct =>
                new SelectListItem
                    { Text = ct.Name, Value = ct.Id, Selected = model.NewsletterCategories.Contains(ct.Id) })
            .ToList();
        model.NewsletterCategories ??= new List<string>();
    }

    protected virtual async Task PrepareEmailAccounts(CampaignModel model)
    {
        //available email accounts
        foreach (var ea in await _emailAccountService.GetAllEmailAccounts())
            model.AvailableEmailAccounts.Add(ea.ToModel());
    }
}