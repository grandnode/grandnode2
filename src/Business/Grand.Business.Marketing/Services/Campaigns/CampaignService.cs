﻿using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Marketing.Interfaces.Campaigns;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Business.Messages.Interfaces;
using Grand.Infrastructure.Extensions;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Messages;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grand.Infrastructure;

namespace Grand.Business.Marketing.Services.Campaigns
{
    public partial class CampaignService : ICampaignService
    {
        private readonly IRepository<Campaign> _campaignRepository;
        private readonly IRepository<CampaignHistory> _campaignHistoryRepository;
        private readonly IRepository<NewsLetterSubscription> _newsLetterSubscriptionRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IEmailSender _emailSender;
        private readonly IQueuedEmailService _queuedEmailService;
        private readonly ICustomerService _customerService;
        private readonly IStoreService _storeService;
        private readonly IMediator _mediator;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;
        private readonly IWorkContext _workContext;

        public CampaignService(IRepository<Campaign> campaignRepository,
            IRepository<CampaignHistory> campaignHistoryRepository,
            IRepository<NewsLetterSubscription> newsLetterSubscriptionRepository,
            IRepository<Customer> customerRepository,
            IEmailSender emailSender, 
            IQueuedEmailService queuedEmailService,
            ICustomerService customerService, IStoreService storeService,
            IMediator mediator,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            ILanguageService languageService,
            IWorkContext workContext)
        {
            _campaignRepository = campaignRepository;
            _campaignHistoryRepository = campaignHistoryRepository;
            _newsLetterSubscriptionRepository = newsLetterSubscriptionRepository;
            _customerRepository = customerRepository;
            _emailSender = emailSender;
            _queuedEmailService = queuedEmailService;
            _storeService = storeService;
            _customerService = customerService;
            _mediator = mediator;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _languageService = languageService;
            _workContext = workContext;
        }

        /// <summary>
        /// Inserts a campaign
        /// </summary>
        /// <param name="campaign">Campaign</param>        
        public virtual async Task InsertCampaign(Campaign campaign)
        {
            if (campaign == null)
                throw new ArgumentNullException(nameof(campaign));

            await _campaignRepository.InsertAsync(campaign);

            //event notification
            await _mediator.EntityInserted(campaign);
        }

        /// <summary>
        /// Inserts a campaign history
        /// </summary>
        /// <param name="campaign">Campaign</param>        
        public virtual async Task InsertCampaignHistory(CampaignHistory campaignhistory)
        {
            if (campaignhistory == null)
                throw new ArgumentNullException(nameof(campaignhistory));

            await _campaignHistoryRepository.InsertAsync(campaignhistory);

        }

        /// <summary>
        /// Updates a campaign
        /// </summary>
        /// <param name="campaign">Campaign</param>
        public virtual async Task UpdateCampaign(Campaign campaign)
        {
            if (campaign == null)
                throw new ArgumentNullException(nameof(campaign));

            await _campaignRepository.UpdateAsync(campaign);

            //event notification
            await _mediator.EntityUpdated(campaign);
        }

        /// <summary>
        /// Deleted a queued email
        /// </summary>
        /// <param name="campaign">Campaign</param>
        public virtual async Task DeleteCampaign(Campaign campaign)
        {
            if (campaign == null)
                throw new ArgumentNullException(nameof(campaign));

            await _campaignRepository.DeleteAsync(campaign);

            //event notification
            await _mediator.EntityDeleted(campaign);
        }

        /// <summary>
        /// Gets a campaign by identifier
        /// </summary>
        /// <param name="campaignId">Campaign identifier</param>
        /// <returns>Campaign</returns>
        public virtual Task<Campaign> GetCampaignById(string campaignId)
        {
            return _campaignRepository.GetByIdAsync(campaignId);

        }

        /// <summary>
        /// Gets all campaigns
        /// </summary>
        /// <returns>Campaigns</returns>
        public virtual async Task<IList<Campaign>> GetAllCampaigns()
        {

            var query = from c in _campaignRepository.Table
                        orderby c.CreatedOnUtc
                        select c;
            return await Task.FromResult(query.ToList());
        }

        public virtual async Task<IPagedList<CampaignHistory>> GetCampaignHistory(Campaign campaign, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            if (campaign == null)
                throw new ArgumentNullException(nameof(campaign));

            var query = from c in _campaignHistoryRepository.Table
                        where c.CampaignId == campaign.Id
                        orderby c.CreatedDateUtc descending
                        select c;
            return await PagedList<CampaignHistory>.Create(query, pageIndex, pageSize);
        }
        public virtual async Task<IPagedList<NewsLetterSubscription>> CustomerSubscriptions(Campaign campaign, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            if (campaign == null)
                throw new ArgumentNullException(nameof(campaign));

            var model = new PagedList<NewsLetterSubscription>();
            if (campaign.CustomerCreatedDateFrom.HasValue || campaign.CustomerCreatedDateTo.HasValue ||
                campaign.CustomerHasShoppingCart != CampaignCondition.All || campaign.CustomerHasShoppingCart != CampaignCondition.All ||
                campaign.CustomerLastActivityDateFrom.HasValue || campaign.CustomerLastActivityDateTo.HasValue ||
                campaign.CustomerLastPurchaseDateFrom.HasValue || campaign.CustomerLastPurchaseDateTo.HasValue ||
                campaign.CustomerTags.Count > 0 || campaign.CustomerGroups.Count > 0)
            {

                var query = from o in _newsLetterSubscriptionRepository.Table
                            where o.Active && o.CustomerId != "" && (o.StoreId == campaign.StoreId || String.IsNullOrEmpty(campaign.StoreId))
                            join c in _customerRepository.Table on o.CustomerId equals c.Id into joined
                            from customers in joined
                            select new CampaignCustomerHelp()
                            {
                                CustomerEmail = customers.Email,
                                Email = o.Email,
                                CustomerId = customers.Id,
                                CreatedOnUtc = customers.CreatedOnUtc,
                                CustomerTags = customers.CustomerTags,
                                CustomerGroups = customers.Groups,
                                NewsletterCategories = o.Categories,
                                HasShoppingCartItems = customers.ShoppingCartItems.Any(),
                                LastActivityDateUtc = customers.LastActivityDateUtc,
                                LastPurchaseDateUtc = customers.LastPurchaseDateUtc,
                                NewsLetterSubscriptionGuid = o.NewsLetterSubscriptionGuid
                            };

                //create date
                if (campaign.CustomerCreatedDateFrom.HasValue)
                    query = query.Where(x => x.CreatedOnUtc >= campaign.CustomerCreatedDateFrom.Value);
                if (campaign.CustomerCreatedDateTo.HasValue)
                    query = query.Where(x => x.CreatedOnUtc <= campaign.CustomerCreatedDateTo.Value);

                //last activity
                if (campaign.CustomerLastActivityDateFrom.HasValue)
                    query = query.Where(x => x.LastActivityDateUtc >= campaign.CustomerLastActivityDateFrom.Value);
                if (campaign.CustomerLastActivityDateTo.HasValue)
                    query = query.Where(x => x.LastActivityDateUtc <= campaign.CustomerLastActivityDateTo.Value);

                //last purchase
                if (campaign.CustomerLastPurchaseDateFrom.HasValue)
                    query = query.Where(x => x.LastPurchaseDateUtc >= campaign.CustomerLastPurchaseDateFrom.Value);
                if (campaign.CustomerLastPurchaseDateTo.HasValue)
                    query = query.Where(x => x.LastPurchaseDateUtc <= campaign.CustomerLastPurchaseDateTo.Value);

                //customer has shopping carts
                if (campaign.CustomerHasShoppingCart == CampaignCondition.True)
                    query = query.Where(x => x.HasShoppingCartItems);
                if (campaign.CustomerHasShoppingCart == CampaignCondition.False)
                    query = query.Where(x => !x.HasShoppingCartItems);

                //customer has order
                if (campaign.CustomerHasOrders == CampaignCondition.True)
                    query = query.Where(x => x.IsHasOrders);
                if (campaign.CustomerHasOrders == CampaignCondition.False)
                    query = query.Where(x => !x.IsHasOrders);

                //tags
                if (campaign.CustomerTags.Count > 0)
                {
                    foreach (var item in campaign.CustomerTags)
                    {
                        query = query.Where(x => x.CustomerTags.Contains(item));
                    }
                }
                //roles
                if (campaign.CustomerGroups.Count > 0)
                {
                    foreach (var item in campaign.CustomerGroups)
                    {
                        query = query.Where(x => x.CustomerGroups.Any(z => z == item));
                    }
                }
                //categories news
                if (campaign.NewsletterCategories.Count > 0)
                {
                    foreach (var item in campaign.NewsletterCategories)
                    {
                        query = query.Where(x => x.NewsletterCategories.Contains(item));
                    }
                }
                model = await PagedList<NewsLetterSubscription>.Create(query.Select(x => new NewsLetterSubscription() { CustomerId = x.CustomerId, Email = x.Email, NewsLetterSubscriptionGuid = x.NewsLetterSubscriptionGuid }), pageIndex, pageSize);
            }
            else
            {
                var query = from o in _newsLetterSubscriptionRepository.Table
                            where o.Active && (o.StoreId == campaign.StoreId || string.IsNullOrEmpty(campaign.StoreId))
                            select o;

                if (campaign.NewsletterCategories.Count > 0)
                {
                    foreach (var item in campaign.NewsletterCategories)
                    {
                        query = query.Where(x => x.Categories.Contains(item));
                    }
                }
                model = await PagedList<NewsLetterSubscription>.Create(query, pageIndex, pageSize);
            }

            return await Task.FromResult(model);
        }

        private class CampaignCustomerHelp
        {
            public CampaignCustomerHelp()
            {
                CustomerGroups = new List<string>();
            }
            public string CustomerId { get; set; }
            public string CustomerEmail { get; set; }
            public string Email { get; set; }
            public DateTime CreatedOnUtc { get; set; }
            public DateTime LastActivityDateUtc { get; set; }
            public DateTime? LastPurchaseDateUtc { get; set; }
            public bool HasShoppingCartItems { get; set; }
            public bool IsHasOrders { get; set; }
            public ICollection<string> CustomerTags { get; set; }
            public ICollection<string> NewsletterCategories { get; set; }
            public ICollection<string> CustomerGroups { get; set; }
            public Guid NewsLetterSubscriptionGuid { get; set; }
        }

        /// <summary>
        /// Sends a campaign to specified emails
        /// </summary>
        /// <param name="campaign">Campaign</param>
        /// <param name="emailAccount">Email account</param>
        /// <param name="subscriptions">Subscriptions</param>
        /// <returns>Total emails sent</returns>
        public virtual async Task<int> SendCampaign(Campaign campaign, EmailAccount emailAccount,
            IEnumerable<NewsLetterSubscription> subscriptions)
        {
            if (campaign == null)
                throw new ArgumentNullException(nameof(campaign));

            if (emailAccount == null)
                throw new ArgumentNullException(nameof(emailAccount));

            int totalEmailsSent = 0;
            var language = await _languageService.GetLanguageById(campaign.LanguageId);
            if (language == null)
                language = (await _languageService.GetAllLanguages()).FirstOrDefault();

            foreach (var subscription in subscriptions)
            {
                Customer customer = null;

                if (!String.IsNullOrEmpty(subscription.CustomerId))
                {
                    customer = await _customerService.GetCustomerById(subscription.CustomerId);
                }

                if (customer == null)
                {
                    customer = await _customerService.GetCustomerByEmail(subscription.Email);
                }

                //ignore deleted or inactive customers when sending newsletter campaigns
                if (customer != null && (!customer.Active || customer.Deleted))
                    continue;

                var builder = new LiquidObjectBuilder(_mediator);
                var store = await _storeService.GetStoreById(campaign.StoreId);
                if (store == null)
                    store = (await _storeService.GetAllStores()).FirstOrDefault();

                builder.AddStoreTokens(store, language, emailAccount)
                       .AddNewsLetterSubscriptionTokens(subscription, store, _workContext.CurrentHost);

                if (customer != null)
                {
                    builder.AddCustomerTokens(customer, store, _workContext.CurrentHost, language)
                           .AddShoppingCartTokens(customer, store, language);
                }

                var email = new QueuedEmail();

                var liquidObject = await builder.BuildAsync();
                liquidObject.Email = new LiquidEmail(email.Id);

                var body = LiquidExtensions.Render(liquidObject, campaign.Body);
                var subject = LiquidExtensions.Render(liquidObject, campaign.Subject);

                email.PriorityId = QueuedEmailPriority.Low;
                email.From = emailAccount.Email;
                email.FromName = emailAccount.DisplayName;
                email.To = subscription.Email;
                email.Subject = subject;
                email.Body = body;
                email.CreatedOnUtc = DateTime.UtcNow;
                email.EmailAccountId = emailAccount.Id;
                email.Reference = Domain.Common.Reference.Campaign;
                email.ObjectId = campaign.Id;

                await _queuedEmailService.InsertQueuedEmail(email);
                await InsertCampaignHistory(new CampaignHistory() { CampaignId = campaign.Id, CustomerId = subscription.CustomerId, Email = subscription.Email, CreatedDateUtc = DateTime.UtcNow, StoreId = campaign.StoreId });

                //activity log
                if (customer != null)
                    _ = _customerActivityService.InsertActivity("CustomerReminder.SendCampaign", campaign.Id, customer, "", _translationService.GetResource("ActivityLog.SendCampaign"), campaign.Name);

                totalEmailsSent++;
            }
            return totalEmailsSent;
        }

        /// <summary>
        /// Sends a campaign to specified email
        /// </summary>
        /// <param name="campaign">Campaign</param>
        /// <param name="emailAccount">Email account</param>
        /// <param name="email">Email</param>
        public virtual async Task SendCampaign(Campaign campaign, EmailAccount emailAccount, string email)
        {
            if (campaign == null)
                throw new ArgumentNullException(nameof(campaign));

            if (emailAccount == null)
                throw new ArgumentNullException(nameof(emailAccount));

            var language = await _languageService.GetLanguageById(campaign.LanguageId);
            if (language == null)
                language = (await _languageService.GetAllLanguages()).FirstOrDefault();

            var store = await _storeService.GetStoreById(campaign.StoreId);
            if (store == null)
                store = (await _storeService.GetAllStores()).FirstOrDefault();

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount);
            var customer = await _customerService.GetCustomerByEmail(email);
            if (customer != null)
            {
                builder.AddCustomerTokens(customer, store, _workContext.CurrentHost, language)
                       .AddShoppingCartTokens(customer, store, language);
            }

            var liquidObject = await builder.BuildAsync();
            var body = LiquidExtensions.Render(liquidObject, campaign.Body);
            var subject = LiquidExtensions.Render(liquidObject, campaign.Subject);

            await _emailSender.SendEmail(emailAccount, subject, body, emailAccount.Email, emailAccount.DisplayName, email, null);
        }
    }
}