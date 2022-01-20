﻿using Grand.Domain.Catalog;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Grand.Web.Common.Models;
using Grand.Domain.Common;

namespace Grand.Web.Admin.Models.Customers
{
    public partial class CustomerModel : BaseEntityModel
    {
        public CustomerModel()
        {
            SendEmail = new SendEmailModel() { SendImmediately = true };
            AssociatedExternalAuthRecords = new List<AssociatedExternalAuthModel>();
            AvailableCountries = new List<SelectListItem>();
            AvailableStates = new List<SelectListItem>();
            AvailableStores = new List<SelectListItem>();
            AvailableSalesEmployees = new List<SelectListItem>();
            CustomerAttributes = new List<CustomerAttributeModel>();
            AvailableNewsletterSubscriptionStores = new List<StoreModel>();
            LoyaltyPointsAvailableStores = new List<SelectListItem>();
            Attributes = new List<CustomAttribute>();
        }

        public bool AllowUsersToChangeUsernames { get; set; }
        public bool UsernamesEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.Username")]
        public string Username { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.Email")]
        public string Email { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.Owner")]
        public string Owner { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [UIHint("CustomerGroups")]
        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.CustomerGroups")]
        public string[] CustomerGroups { get; set; }
        //customer groups
        public string CustomerGroupNames { get; set; }
        
        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.CustomerTags")]
        public string CustomerTags { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.Vendor")]
        [UIHint("Vendor")]
        public string VendorId { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.SeId")]
        public string SeId { get; set; }
        public IList<SelectListItem> AvailableSalesEmployees { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.StaffStore")]
        public string StaffStoreId { get; set; }
        public IList<SelectListItem> AvailableStores { get; set; }

        //form fields & properties
        public bool GenderEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.Gender")]
        public string Gender { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.FirstName")]
        public string FirstName { get; set; }
        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.LastName")]

        public string LastName { get; set; }
        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.FullName")]
        public string FullName { get; set; }

        public bool DateOfBirthEnabled { get; set; }
        [UIHint("DateNullable")]
        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.DateOfBirth")]
        public DateTime? DateOfBirth { get; set; }

        public bool CompanyEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.Company")]
        public string Company { get; set; }

        public bool StreetAddressEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.StreetAddress")]
        public string StreetAddress { get; set; }

        public bool StreetAddress2Enabled { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.StreetAddress2")]
        public string StreetAddress2 { get; set; }

        public bool ZipPostalCodeEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.ZipPostalCode")]
        public string ZipPostalCode { get; set; }

        public bool CityEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.City")]
        public string City { get; set; }

        public bool CountryEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.Country")]
        public string CountryId { get; set; }
        public IList<SelectListItem> AvailableCountries { get; set; }

        public bool StateProvinceEnabled { get; set; }
        
        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.StateProvince")]
        public string StateProvinceId { get; set; }
        public IList<SelectListItem> AvailableStates { get; set; }

        public bool PhoneEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.Phone")]
        public string Phone { get; set; }

        public bool FaxEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.Fax")]
        public string Fax { get; set; }

        public List<CustomerAttributeModel> CustomerAttributes { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.AdminComment")]
        public string AdminComment { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.IsTaxExempt")]
        public bool IsTaxExempt { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.TwoFactorEnabled")]
        public bool TwoFactorEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.FreeShipping")]
        public bool FreeShipping { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.Active")]
        public bool Active { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.Affiliate")]
        public string AffiliateId { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.Affiliate")]
        public string AffiliateName { get; set; }
        
        public IList<CustomAttribute> Attributes { get; set; }

        //EU VAT
        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.VatNumber")]
        public string VatNumber { get; set; }

        public string VatNumberStatusNote { get; set; }

        public bool DisplayVatNumber { get; set; }

        //registration date
        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.CreatedOn")]
        public DateTime CreatedOn { get; set; }
        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.LastActivityDate")]
        public DateTime LastActivityDate { get; set; }
        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.LastPurchaseDate")]
        public DateTime? LastPurchaseDate { get; set; }

        //IP adderss
        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.IPAddress")]
        public string LastIpAddress { get; set; }

        //Url referrer
        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.URLReferrer")]
        public string UrlReferrer { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.LastVisitedPage")]
        public string LastVisitedPage { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.LastUrlReferrer")]
        public string LastUrlReferrer { get; set; }

        //newsletter subscriptions (per store)
        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.Newsletter")]
        public List<StoreModel> AvailableNewsletterSubscriptionStores { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.Fields.Newsletter")]
        public string[] SelectedNewsletterSubscriptionStoreIds { get; set; }

        //loyalty points history
        public bool DisplayLoyaltyPointsHistory { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.LoyaltyPoints.Fields.AddLoyaltyPointsValue")]
        public int AddLoyaltyPointsValue { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.LoyaltyPoints.Fields.AddLoyaltyPointsMessage")]
        public string AddLoyaltyPointsMessage { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.LoyaltyPoints.Fields.AddLoyaltyPointsStore")]
        public string AddLoyaltyPointsStoreId { get; set; }

        [GrandResourceDisplayName("Admin.Customers.Customers.LoyaltyPoints.Fields.AddLoyaltyPointsStore")]
        public IList<SelectListItem> LoyaltyPointsAvailableStores { get; set; }

        //send email model
        public SendEmailModel SendEmail { get; set; }

        //send the welcome message
        public bool AllowSendingOfWelcomeMessage { get; set; }
        //re-send the activation message
        public bool AllowReSendingOfActivationMessage { get; set; }
        public bool ShowMessageContactForm { get; set; }

        //external auth
        [GrandResourceDisplayName("Admin.Customers.Customers.AssociatedExternalAuth")]
        public IList<AssociatedExternalAuthModel> AssociatedExternalAuthRecords { get; set; }
        //customer notes
        [GrandResourceDisplayName("Admin.Customers.CustomerNotes.Fields.DisplayToCustomer")]
        public bool AddCustomerNoteDisplayToCustomer { get; set; }
        [GrandResourceDisplayName("Admin.Customers.CustomerNotes.Fields.Title")]
        public string AddCustomerTitle { get; set; }
        [GrandResourceDisplayName("Admin.Customers.CustomerNotes.Fields.Note")]
        public string AddCustomerNoteMessage { get; set; }
        public bool AddCustomerNoteHasDownload { get; set; }
        [GrandResourceDisplayName("Admin.Customers.CustomerNotes.Fields.Download")]
        [UIHint("Download")]
        public string AddCustomerNoteDownloadId { get; set; }

        #region Nested classes

        public partial class AssociatedExternalAuthModel : BaseEntityModel
        {
            [GrandResourceDisplayName("Admin.Customers.Customers.AssociatedExternalAuth.Fields.Email")]
            public string Email { get; set; }

            [GrandResourceDisplayName("Admin.Customers.Customers.AssociatedExternalAuth.Fields.ExternalIdentifier")]
            public string ExternalIdentifier { get; set; }

            [GrandResourceDisplayName("Admin.Customers.Customers.AssociatedExternalAuth.Fields.AuthMethodName")]
            public string AuthMethodName { get; set; }
        }

        public partial class LoyaltyPointsHistoryModel : BaseEntityModel
        {
            [GrandResourceDisplayName("Admin.Customers.Customers.LoyaltyPoints.Fields.Store")]
            public string StoreName { get; set; }

            [GrandResourceDisplayName("Admin.Customers.Customers.LoyaltyPoints.Fields.Points")]
            public int Points { get; set; }

            [GrandResourceDisplayName("Admin.Customers.Customers.LoyaltyPoints.Fields.PointsBalance")]
            public int PointsBalance { get; set; }

            [GrandResourceDisplayName("Admin.Customers.Customers.LoyaltyPoints.Fields.Message")]
            public string Message { get; set; }

            [GrandResourceDisplayName("Admin.Customers.Customers.LoyaltyPoints.Fields.Date")]
            public DateTime CreatedOn { get; set; }
        }

        public partial class SendEmailModel : BaseModel
        {
            [GrandResourceDisplayName("Admin.Customers.Customers.SendEmail.Subject")]
            public string Subject { get; set; }

            [GrandResourceDisplayName("Admin.Customers.Customers.SendEmail.Body")]
            public string Body { get; set; }
            [GrandResourceDisplayName("Admin.Customers.Customers.SendEmail.SendImmediately")]
            public bool SendImmediately { get; set; }

            [GrandResourceDisplayName("Admin.Customers.Customers.SendEmail.DontSendBeforeDate")]
            [UIHint("DateTimeNullable")]
            public DateTime? DontSendBeforeDate { get; set; }
        }

        public partial class OrderModel : BaseEntityModel
        {
            [GrandResourceDisplayName("Admin.Customers.Customers.Orders.ID")]
            public override string Id { get; set; }
            [GrandResourceDisplayName("Admin.Customers.Customers.Orders.ID")]
            public int OrderNumber { get; set; }
            [GrandResourceDisplayName("Admin.Customers.Customers.Orders.Code")]
            public string OrderCode { get; set; }
            [GrandResourceDisplayName("Admin.Customers.Customers.Orders.OrderStatus")]
            public string OrderStatus { get; set; }
            public int OrderStatusId { get; set; }
            [GrandResourceDisplayName("Admin.Customers.Customers.Orders.PaymentStatus")]
            public string PaymentStatus { get; set; }
            public int PaymentStatusId { get; set; }
            [GrandResourceDisplayName("Admin.Customers.Customers.Orders.ShippingStatus")]
            public string ShippingStatus { get; set; }
            public int ShippingStatusId { get; set; }
            [GrandResourceDisplayName("Admin.Customers.Customers.Orders.OrderTotal")]
            public string OrderTotal { get; set; }
            [GrandResourceDisplayName("Admin.Customers.Customers.Orders.Store")]
            public string StoreName { get; set; }
            [GrandResourceDisplayName("Admin.Customers.Customers.Orders.CreatedOn")]
            public DateTime CreatedOn { get; set; }
        }

        public partial class ActivityLogModel : BaseEntityModel
        {
            [GrandResourceDisplayName("Admin.Customers.Customers.ActivityLog.ActivityLogType")]
            public string ActivityLogTypeName { get; set; }
            [GrandResourceDisplayName("Admin.Customers.Customers.ActivityLog.Comment")]
            public string Comment { get; set; }
            [GrandResourceDisplayName("Admin.Customers.Customers.ActivityLog.CreatedOn")]
            public DateTime CreatedOn { get; set; }
            [GrandResourceDisplayName("Admin.Customers.Customers.ActivityLog.IpAddress")]
            public string IpAddress { get; set; }
        }
        public partial class ProductModel : BaseEntityModel
        {
            [GrandResourceDisplayName("Admin.Customers.Customers.PersonalizedProduct.ProductName")]
            public string ProductName { get; set; }
            [GrandResourceDisplayName("Admin.Customers.Customers.PersonalizedProduct.DisplayOrder")]
            public int DisplayOrder { get; set; }
            public string ProductId { get; set; }
        }
        public partial class ProductPriceModel : BaseEntityModel
        {
            [GrandResourceDisplayName("Admin.Customers.Customers.ProductPrice.ProductName")]
            public string ProductName { get; set; }
            [GrandResourceDisplayName("Admin.Customers.Customers.ProductPrice.Price")]
            public double Price { get; set; }
            public string ProductId { get; set; }
        }
        public partial class AddProductModel : BaseModel
        {
            public AddProductModel()
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

            public string CustomerId { get; set; }

            public string[] SelectedProductIds { get; set; }
        }
        public partial class OutOfStockSubscriptionModel : BaseEntityModel
        {
            [GrandResourceDisplayName("Admin.Customers.Customers.OutOfStockSubscriptions.Store")]
            public string StoreName { get; set; }
            [GrandResourceDisplayName("Admin.Customers.Customers.OutOfStockSubscriptions.Product")]
            public string ProductId { get; set; }
            [GrandResourceDisplayName("Admin.Customers.Customers.OutOfStockSubscriptions.Product")]
            public string ProductName { get; set; }
            public string AttributeDescription { get; set; }
            [GrandResourceDisplayName("Admin.Customers.Customers.OutOfStockSubscriptions.CreatedOn")]
            public DateTime CreatedOn { get; set; }
        }

        public partial class CustomerAttributeModel : BaseEntityModel
        {
            public CustomerAttributeModel()
            {
                Values = new List<CustomerAttributeValueModel>();
            }

            public string Name { get; set; }

            public bool IsRequired { get; set; }

            /// <summary>
            /// Default value for textboxes
            /// </summary>
            public string DefaultValue { get; set; }

            public AttributeControlType AttributeControlType { get; set; }

            public IList<CustomerAttributeValueModel> Values { get; set; }

        }

        public partial class CustomerAttributeValueModel : BaseEntityModel
        {
            public string Name { get; set; }

            public bool IsPreSelected { get; set; }
        }

        public partial class CustomerNote : BaseEntityModel
        {
            public string CustomerId { get; set; }
            [GrandResourceDisplayName("Admin.Customers.CustomerNotes.Fields.DisplayToCustomer")]
            public bool DisplayToCustomer { get; set; }
            [GrandResourceDisplayName("Admin.Customers.CustomerNotes.Fields.Title")]
            public string Title { get; set; }
            [GrandResourceDisplayName("Admin.Customers.CustomerNotes.Fields.Note")]
            public string Note { get; set; }
            [GrandResourceDisplayName("Admin.Customers.CustomerNotes.Fields.Download")]
            public string DownloadId { get; set; }
            public Guid DownloadGuid { get; set; }
            [GrandResourceDisplayName("Admin.Customers.CustomerNotes.Fields.CreatedOn")]
            public DateTime CreatedOn { get; set; }
        }

        #endregion
    }
}