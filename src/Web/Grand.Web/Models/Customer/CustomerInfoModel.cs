﻿using Grand.Domain.Customers;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Binders;
using Grand.Web.Common.Models;
using Grand.Web.Models.Newsletter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Models.Customer
{
    public class CustomerInfoModel : BaseModel
    {
        public CustomerInfoModel()
        {
            AvailableCountries = new List<SelectListItem>();
            AvailableStates = new List<SelectListItem>();
            AssociatedExternalAuthRecords = new List<AssociatedExternalAuthModel>();
            CustomerAttributes = new List<CustomerAttributeModel>();
            SelectedAttributes = new List<CustomAttributeModel>();
            NewsletterCategories = new List<NewsletterSimpleCategory>();
        }

        [DataType(DataType.EmailAddress)]
        [GrandResourceDisplayName("Account.Fields.Email")]
        public string Email { get; set; }
        public bool AllowUsersToChangeEmail { get; set; }

        public bool CheckUsernameAvailabilityEnabled { get; set; }
        public bool AllowUsersToChangeUsernames { get; set; }
        public bool UsernamesEnabled { get; set; }
        [GrandResourceDisplayName("Account.Fields.Username")]
        public string Username { get; set; }

        //form fields & properties
        public bool GenderEnabled { get; set; }
        [GrandResourceDisplayName("Account.Fields.Gender")]
        public string Gender { get; set; }

        [GrandResourceDisplayName("Account.Fields.FirstName")]
        public string FirstName { get; set; }
        [GrandResourceDisplayName("Account.Fields.LastName")]
        public string LastName { get; set; }
        public bool FirstLastNameRequired { get; set; }
        public bool DateOfBirthEnabled { get; set; }
        [GrandResourceDisplayName("Account.Fields.DateOfBirth")]
        public int? DateOfBirthDay { get; set; }
        [GrandResourceDisplayName("Account.Fields.DateOfBirth")]
        public int? DateOfBirthMonth { get; set; }
        [GrandResourceDisplayName("Account.Fields.DateOfBirth")]
        public int? DateOfBirthYear { get; set; }
        public bool DateOfBirthRequired { get; set; }
        public DateTime? ParseDateOfBirth()
        {
            if (!DateOfBirthYear.HasValue || !DateOfBirthMonth.HasValue || !DateOfBirthDay.HasValue)
                return null;

            DateTime? dateOfBirth = null;
            try
            {
                dateOfBirth = new DateTime(DateOfBirthYear.Value, DateOfBirthMonth.Value, DateOfBirthDay.Value);
            }
            catch { }
            return dateOfBirth;
        }

        public bool CompanyEnabled { get; set; }
        public bool CompanyRequired { get; set; }
        [GrandResourceDisplayName("Account.Fields.Company")]
        public string Company { get; set; }

        public bool StreetAddressEnabled { get; set; }
        public bool StreetAddressRequired { get; set; }
        [GrandResourceDisplayName("Account.Fields.StreetAddress")]
        public string StreetAddress { get; set; }

        public bool StreetAddress2Enabled { get; set; }
        public bool StreetAddress2Required { get; set; }
        [GrandResourceDisplayName("Account.Fields.StreetAddress2")]
        public string StreetAddress2 { get; set; }

        public bool ZipPostalCodeEnabled { get; set; }
        public bool ZipPostalCodeRequired { get; set; }
        [GrandResourceDisplayName("Account.Fields.ZipPostalCode")]
        public string ZipPostalCode { get; set; }

        public bool CityEnabled { get; set; }
        public bool CityRequired { get; set; }
        [GrandResourceDisplayName("Account.Fields.City")]
        public string City { get; set; }

        public bool CountryEnabled { get; set; }
        public bool CountryRequired { get; set; }
        [GrandResourceDisplayName("Account.Fields.Country")]
        public string CountryId { get; set; }
        public IList<SelectListItem> AvailableCountries { get; set; }

        public bool StateProvinceEnabled { get; set; }
        public bool StateProvinceRequired { get; set; }
        [GrandResourceDisplayName("Account.Fields.StateProvince")]
        public string StateProvinceId { get; set; }
        public IList<SelectListItem> AvailableStates { get; set; }

        public bool PhoneEnabled { get; set; }
        public bool PhoneRequired { get; set; }
        [DataType(DataType.PhoneNumber)]
        [GrandResourceDisplayName("Account.Fields.Phone")]
        public string Phone { get; set; }

        public bool FaxEnabled { get; set; }
        public bool FaxRequired { get; set; }

        [GrandResourceDisplayName("Account.Fields.Fax")]
        public string Fax { get; set; }

        public bool NewsletterEnabled { get; set; }
        [GrandResourceDisplayName("Account.Fields.Newsletter")]
        public bool Newsletter { get; set; }
        public string[] SelectedNewsletterCategory { get; set; }
        
        //2factory
        public bool Is2faEnabled { get; set; }

        //EU VAT
        [GrandResourceDisplayName("Account.Fields.VatNumber")]
        public string VatNumber { get; set; }
        public string VatNumberStatusNote { get; set; }
        public bool DisplayVatNumber { get; set; }

        //external authentication
        [GrandResourceDisplayName("Account.AssociatedExternalAuth")]
        public IList<AssociatedExternalAuthModel> AssociatedExternalAuthRecords { get; set; }
        public int NumberOfExternalAuthenticationProviders { get; set; }
        
        [ModelBinder(BinderType = typeof(CustomAttributesBinder))]
        public IList<CustomAttributeModel> SelectedAttributes { get; set; }
        
        public IList<CustomerAttributeModel> CustomerAttributes { get; set; }

        public IList<NewsletterSimpleCategory> NewsletterCategories { get; set; }


        #region Nested classes

        public class AssociatedExternalAuthModel : BaseEntityModel
        {
            public string Email { get; set; }
            public string ExternalIdentifier { get; set; }
            public string AuthMethodName { get; set; }
        }

        public class TwoFactorAuthenticationModel : BaseModel
        {
            public TwoFactorAuthenticationModel()
            {
                CustomValues = new Dictionary<string, string>();
            }
            public TwoFactorAuthenticationType TwoFactorAuthenticationType { get; set; }
            public string SecretKey { get; set; }
            public string Code { get; set; }
            public IDictionary<string, string> CustomValues { get; set; }
        }

        public class TwoFactorAuthorizationModel : BaseModel
        {
            public string Code { get; set; }
            public TwoFactorAuthenticationType TwoFactorAuthenticationType { get; set; }
        }

        #endregion
    }
}