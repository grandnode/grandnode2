using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Localization;
using Grand.Domain.Permissions;
using Grand.Domain.Stores;

namespace Grand.Domain.Orders
{
    /// <summary>
    /// Represents a checkout attribute
    /// </summary>
    public partial class CheckoutAttribute : BaseEntity, ITranslationEntity, IStoreLinkEntity, IGroupLinkEntity
    {
        private ICollection<CheckoutAttributeValue> _checkoutAttributeValues;

        public CheckoutAttribute()
        {
            Stores = new List<string>();
            Locales = new List<TranslationEntity>();
            CustomerGroups = new List<string>();
            ConditionAttribute = new List<CustomAttribute>();
        }
        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the text prompt
        /// </summary>
        public string TextPrompt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is required
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether shippable products are required in order to display this attribute
        /// </summary>
        public bool ShippableProductRequired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the attribute is marked as tax exempt
        /// </summary>
        public bool IsTaxExempt { get; set; }

        /// <summary>
        /// Gets or sets the tax category identifier
        /// </summary>
        public string TaxCategoryId { get; set; }

        /// <summary>
        /// Gets or sets the attribute control type identifier
        /// </summary>
        public AttributeControlType AttributeControlTypeId { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores
        /// </summary>
        public bool LimitedToStores { get; set; }
        public IList<string> Stores { get; set; }


        /// <summary>
        /// Gets or sets the collection of locales
        /// </summary>
        public IList<TranslationEntity> Locales { get; set; }

        //validation fields

        /// <summary>
        /// Gets or sets the validation rule for minimum length (for textbox and multiline textbox)
        /// </summary>
        public int? ValidationMinLength { get; set; }

        /// <summary>
        /// Gets or sets the validation rule for maximum length (for textbox and multiline textbox)
        /// </summary>
        public int? ValidationMaxLength { get; set; }

        /// <summary>
        /// Gets or sets the validation rule for file allowed extensions (for file upload)
        /// </summary>
        public string ValidationFileAllowedExtensions { get; set; }

        /// <summary>
        /// Gets or sets the validation rule for file maximum size in kilobytes (for file upload)
        /// </summary>
        public int? ValidationFileMaximumSize { get; set; }

        /// <summary>
        /// Gets or sets the default value (for textbox and multiline textbox)
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets a condition (depending on other attribute) when this attribute should be enabled (visible).
        /// </summary>
        public IList<CustomAttribute> ConditionAttribute { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the entity is subject to ACL
        /// </summary>
        public bool LimitedToGroups { get; set; }
        public IList<string> CustomerGroups { get; set; }

        /// <summary>
        /// Gets the checkout attribute values
        /// </summary>
        public virtual ICollection<CheckoutAttributeValue> CheckoutAttributeValues
        {
            get { return _checkoutAttributeValues ??= new List<CheckoutAttributeValue>(); }
            protected set { _checkoutAttributeValues = value; }
        }
    }

}
