using Grand.Domain.Customers;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Customers
{
    public partial class CustomerReminderModel : BaseEntityModel
    {

        [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Fields.StartDate")]
        public DateTime StartDateTime { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Fields.EndDate")]
        public DateTime EndDateTime { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Fields.LastUpdateDate")]
        public DateTime LastUpdateDate { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Fields.AllowRenew")]
        public bool AllowRenew { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Fields.RenewedDay")]
        public int RenewedDay { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Fields.Active")]
        public bool Active { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Fields.ReminderRule")]
        public CustomerReminderRuleEnum ReminderRuleId { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Fields.ConditionId")]
        public int ConditionId { get; set; }
        public int ConditionCount { get; set; }


        public partial class ConditionModel : BaseEntityModel
        {
            public ConditionModel()
            {
                this.ConditionType = new List<SelectListItem>();
            }

            [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Condition.Fields.Name")]
            public string Name { get; set; }

            [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Condition.Fields.ConditionTypeId")]
            public CustomerReminderConditionTypeEnum ConditionTypeId { get; set; }
            public IList<SelectListItem> ConditionType { get; set; }

            [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Condition.Fields.ConditionId")]
            public CustomerReminderConditionEnum ConditionId { get; set; }

            public string CustomerReminderId { get; set; }

            public partial class AddProductToConditionModel
            {
                public AddProductToConditionModel()
                {
                    AvailableStores = new List<SelectListItem>();
                    AvailableVendors = new List<SelectListItem>();
                    AvailableProductTypes = new List<SelectListItem>();
                }

                [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductName")]

                public string SearchProductName { get; set; }
                [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCategory")]
                [UIHint("Category")]
                public string SearchCategoryId { get; set; }
                [GrandResourceDisplayName("Admin.Catalog.Products.List.Brand")]
                [UIHint("Brand")]
                public string SearchBrandId { get; set; }
                [UIHint("Collection")]
                [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCollection")]
                public string SearchCollectionId { get; set; }
                [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchStore")]
                public string SearchStoreId { get; set; }
                [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchVendor")]
                public string SearchVendorId { get; set; }
                [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductType")]
                public int SearchProductTypeId { get; set; }

                public IList<SelectListItem> AvailableStores { get; set; }
                public IList<SelectListItem> AvailableVendors { get; set; }
                public IList<SelectListItem> AvailableProductTypes { get; set; }

                public string CustomerReminderId { get; set; }
                public string ConditionId { get; set; }

                public string[] SelectedProductIds { get; set; }
            }
            public partial class AddCategoryConditionModel
            {
                [GrandResourceDisplayName("Admin.Catalog.Categories.List.SearchCategoryName")]

                public string SearchCategoryName { get; set; }

                public string CustomerReminderId { get; set; }
                public string ConditionId { get; set; }

                public string[] SelectedCategoryIds { get; set; }
            }
            public partial class AddCollectionConditionModel
            {
                [GrandResourceDisplayName("Admin.Catalog.Collections.List.SearchCollectionName")]

                public string SearchCollectionName { get; set; }

                public string CustomerReminderId { get; set; }
                public string ConditionId { get; set; }

                public string[] SelectedCollectionIds { get; set; }
            }
            public partial class AddCustomerGroupConditionModel
            {
                public string CustomerReminderId { get; set; }
                public string ConditionId { get; set; }

                public string CustomerGroupId { get; set; }
                public string Id { get; set; }
            }
            public partial class AddCustomerTagConditionModel
            {
                public string CustomerReminderId { get; set; }
                public string ConditionId { get; set; }

                public string CustomerTagId { get; set; }
                public string Id { get; set; }
            }
            public partial class AddCustomerRegisterConditionModel
            {
                public string CustomerReminderId { get; set; }
                public string ConditionId { get; set; }
                public string CustomerRegisterName { get; set; }
                public string CustomerRegisterValue { get; set; }
                public string Id { get; set; }
            }
            public partial class AddCustomCustomerAttributeConditionModel
            {
                public string Id { get; set; }
                public string CustomerReminderId { get; set; }
                public string ConditionId { get; set; }
                public string CustomerAttributeName { get; set; }
                public string CustomerAttributeValue { get; set; }
            }

        }

        public partial class ReminderLevelModel : BaseEntityModel
        {
            public ReminderLevelModel()
            {
                EmailAccounts = new List<SelectListItem>();
            }

            public string CustomerReminderId { get; set; }

            [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Level.Fields.SendDay")]
            public int Day { get; set; }
            [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Level.Fields.SendHour")]
            public int Hour { get; set; }

            [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Level.Fields.SendMinutes")]
            public int Minutes { get; set; }

            [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Level.Fields.Name")]
            public string Name { get; set; }

            [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Level.Fields.AllowedTokens")]
            public string[] AllowedTokens { get; set; }

            [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Level.Fields.Level")]
            public int Level { get; set; }

            [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Level.Fields.EmailAccountId")]
            public string EmailAccountId { get; set; }
            public IList<SelectListItem> EmailAccounts { get; set; }

            [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Level.Fields.BccEmailAddresses")]
            public string BccEmailAddresses { get; set; }

            [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Level.Fields.Subject")]
            public string Subject { get; set; }

            [GrandResourceDisplayName("Admin.Customers.CustomerReminder.Level.Fields.Body")]

            public string Body { get; set; }
        }

    }



}