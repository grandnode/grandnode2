using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Messages
{
    public partial class CampaignModel : BaseEntityModel
    {
        public CampaignModel()
        {
            AvailableStores = new List<SelectListItem>();
            AvailableLanguages = new List<SelectListItem>();
            AvailableCustomerTags = new List<SelectListItem>();
            CustomerTags = new List<string>();
            NewsletterCategories = new List<string>();
            AvailableCustomerGroups = new List<SelectListItem>();
            CustomerGroups = new List<string>();
            AvailableEmailAccounts = new List<EmailAccountModel>();
        }

        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.Subject")]

        public string Subject { get; set; }

        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.Body")]

        public string Body { get; set; }

        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.Store")]
        public string StoreId { get; set; }
        public IList<SelectListItem> AvailableStores { get; set; }

        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.Language")]
        public string LanguageId { get; set; }
        public IList<SelectListItem> AvailableLanguages { get; set; }


        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.CustomerCreatedDateFrom")]
        [UIHint("DateTimeNullable")]
        public DateTime? CustomerCreatedDateFrom { get; set; }

        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.CustomerCreatedDateTo")]
        [UIHint("DateTimeNullable")]
        public DateTime? CustomerCreatedDateTo { get; set; }

        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.CustomerLastActivityDateFrom")]
        [UIHint("DateTimeNullable")]
        public DateTime? CustomerLastActivityDateFrom { get; set; }
        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.CustomerLastActivityDateTo")]
        [UIHint("DateTimeNullable")]
        public DateTime? CustomerLastActivityDateTo { get; set; }

        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.CustomerLastPurchaseDateFrom")]
        [UIHint("DateTimeNullable")]
        public DateTime? CustomerLastPurchaseDateFrom { get; set; }
        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.CustomerLastPurchaseDateTo")]
        [UIHint("DateTimeNullable")]
        public DateTime? CustomerLastPurchaseDateTo { get; set; }

        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.CustomerHasOrders")]
        public int CustomerHasOrders { get; set; }

        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.CustomerHasShoppingCart")]
        public int CustomerHasShoppingCart { get; set; }

        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.CustomerTags")]
        public IList<SelectListItem> AvailableCustomerTags { get; set; }

        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.CustomerTags")]
        [UIHint("MultiSelect")]
        public IList<string> CustomerTags { get; set; }

        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.NewsletterCategory")]
        [UIHint("MultiSelect")]
        public IList<string> NewsletterCategories { get; set; }
        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.NewsletterCategory")]
        public IList<SelectListItem> AvailableNewsletterCategories { get; set; }

        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.CustomerGroups")]
        public IList<SelectListItem> AvailableCustomerGroups { get; set; }

        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.CustomerGroups")]
        [UIHint("MultiSelect")]
        public IList<string> CustomerGroups { get; set; }


        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.AllowedTokens")]
        public string[] AllowedTokens { get; set; }

        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.EmailAccount")]
        public string EmailAccountId { get; set; }
        public IList<EmailAccountModel> AvailableEmailAccounts { get; set; }

        [GrandResourceDisplayName("admin.marketing.Campaigns.Fields.TestEmail")]

        public string TestEmail { get; set; }
    }
}