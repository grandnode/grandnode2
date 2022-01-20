using Grand.Web.Common.Models;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Models.Stores
{
    public partial class StoreModel : BaseEntityModel, ILocalizedModel<StoreLocalizedModel>
    {
        public StoreModel()
        {
            Locales = new List<StoreLocalizedModel>();
            Domains = new List<DomainHostModel>();            
            AvailableLanguages = new List<SelectListItem>();
            AvailableWarehouses = new List<SelectListItem>();
            AvailableCountries = new List<SelectListItem>();
            AvailableCurrencies = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Admin.Configuration.Stores.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Stores.Fields.Shortcut")]
        public string Shortcut { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Stores.Fields.Url")]
        public string Url { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Stores.Fields.SslEnabled")]
        public virtual bool SslEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Stores.Fields.SecureUrl")]
        public virtual string SecureUrl { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Stores.Fields.DefaultAdminTheme")]
        public string DefaultAdminTheme { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Stores.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Stores.Fields.CompanyName")]
        public string CompanyName { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Stores.Fields.CompanyAddress")]
        public string CompanyAddress { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Stores.Fields.CompanyPhoneNumber")]
        public string CompanyPhoneNumber { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Stores.Fields.CompanyRegNo")]
        public string CompanyRegNo { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Stores.Fields.CompanyVat")]
        public string CompanyVat { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Stores.Fields.CompanyEmail")]
        public string CompanyEmail { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Stores.Fields.CompanyHours")]
        public string CompanyHours { get; set; }

        public IList<StoreLocalizedModel> Locales { get; set; }
        //default language
        [GrandResourceDisplayName("Admin.Configuration.Stores.Fields.DefaultLanguage")]
        public string DefaultLanguageId { get; set; }
        public IList<SelectListItem> AvailableLanguages { get; set; }

        //default warehouse
        [GrandResourceDisplayName("Admin.Configuration.Stores.Fields.DefaultWarehouse")]
        public string DefaultWarehouseId { get; set; }
        public IList<SelectListItem> AvailableWarehouses { get; set; }

        //default country
        [GrandResourceDisplayName("Admin.Configuration.Stores.Fields.DefaultCountry")]
        public string DefaultCountryId { get; set; }
        public IList<SelectListItem> AvailableCountries { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Stores.Fields.DefaultCurrency")]
        public string DefaultCurrencyId { get; set; }
        public IList<SelectListItem> AvailableCurrencies { get; set; }

        public IList<DomainHostModel> Domains { get; set; }

        public BankAccountModel BankAccount { get; set; }

        public class BankAccountModel : BaseEntityModel
        {
            [GrandResourceDisplayName("Admin.Configuration.Stores.BankAccount.Fields.BankCode")]
            public string BankCode { get; set; }
            [GrandResourceDisplayName("Admin.Configuration.Stores.BankAccount.Fields.BankName")]
            public string BankName { get; set; }
            [GrandResourceDisplayName("Admin.Configuration.Stores.BankAccount.Fields.SwiftCode")]
            public string SwiftCode { get; set; }
            [GrandResourceDisplayName("Admin.Configuration.Stores.BankAccount.Fields.AccountNumber")]
            public string AccountNumber { get; set; }
        }
    }

    public partial class StoreLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Stores.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Stores.Fields.Shortcut")]
        public string Shortcut { get; set; }
    }

    public class DomainHostModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Configuration.Stores.Domains.Fields.HostName")]
        public string HostName { get; set; }
        [GrandResourceDisplayName("Admin.Configuration.Stores.Domains.Fields.Url")]
        public string Url { get; set; }
        [GrandResourceDisplayName("Admin.Configuration.Stores.Domains.Fields.Primary")]
        public bool Primary { get; set; }
    }

    
}