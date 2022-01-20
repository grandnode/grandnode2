using Grand.Domain.Localization;

namespace Grand.Domain.Stores
{
    /// <summary>
    /// Represents a store
    /// </summary>
    public partial class Store : BaseEntity, ITranslationEntity
    {
        public Store()
        {
            Locales = new List<TranslationEntity>();
            Domains = new List<DomainHost>();
            BankAccount = new BankAccount();
        }

        /// <summary>
        /// Gets or sets the store name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the store shortcut
        /// </summary>
        public string Shortcut { get; set; }
        
        /// <summary>
        /// Gets or sets the store URL
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SSL is enabled
        /// </summary>
        public bool SslEnabled { get; set; }

        /// <summary>
        /// Gets or sets the store secure URL (HTTPS)
        /// </summary>
        public string SecureUrl { get; set; }

        /// <summary>
        /// Gets or sets the list of domains
        /// </summary>
        public IList<DomainHost> Domains { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the default language for this store; "" is set when we use the default language display order
        /// </summary>
        public string DefaultLanguageId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the default waregouse for this store
        /// </summary>
        public string DefaultWarehouseId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the default country for this store
        /// </summary>
        public string DefaultCountryId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the default currency for this store
        /// </summary>
        public string DefaultCurrencyId { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the company name
        /// </summary>
        public string CompanyName { get; set; }

        /// <summary>
        /// Gets or sets the company registration number
        /// </summary>
        public string CompanyRegNo { get; set; }

        /// <summary>
        /// Gets or sets the company address
        /// </summary>
        public string CompanyAddress { get; set; }

        /// <summary>
        /// Gets or sets the store phone number
        /// </summary>
        public string CompanyPhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the company VAT (used in Europe Union countries)
        /// </summary>
        public string CompanyVat { get; set; }

        /// <summary>
        /// Gets or sets the company email
        /// </summary>
        public string CompanyEmail { get; set; }

        /// <summary>
        /// Gets or sets the company opening hours
        /// </summary>
        public string CompanyHours { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the default admin theme for this store
        /// </summary>
        public string DefaultAdminTheme { get; set; }

        public BankAccount BankAccount { get; set; }

        /// <summary>
        /// Gets or sets the collection of locales
        /// </summary>
        public IList<TranslationEntity> Locales { get; set; }

    }
}
