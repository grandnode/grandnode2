using Grand.Domain.Common;
using Grand.Domain.Localization;
using System;
using System.Collections.Generic;

namespace Grand.Domain.Catalog
{
    /// <summary>
    /// Represents a product attribute mapping
    /// </summary>
    public partial class ProductAttributeMapping : SubBaseEntity, ITranslationEntity
    {
        private ICollection<ProductAttributeValue> _productAttributeValues;

        public ProductAttributeMapping()
        {
            Locales = new List<TranslationEntity>();
            ConditionAttribute = new List<CustomAttribute>();
        }
        
        /// <summary>
        /// Gets or sets the product attribute identifier
        /// </summary>
        public string ProductAttributeId { get; set; }

        /// <summary>
        /// Gets or sets a value a text prompt
        /// </summary>
        public string TextPrompt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is required
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets whether the attribute will be shown on the catalog page
        /// </summary>
        public bool ShowOnCatalogPage { get; set; }

        /// <summary>
        /// Gets or sets whether the attribute will be used in combinations
        /// </summary>
        public bool Combination { get; set; }

        /// <summary>
        /// Gets or sets the attribute control type identifier
        /// </summary>
        public AttributeControlType AttributeControlTypeId { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

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
        /// Gets or sets the custom attributes (see "ProductAttribute" entity for more info)
        /// </summary>
        public IList<CustomAttribute> ConditionAttribute { get; set; }

        /// <summary>
        /// Gets or sets the collection of locales
        /// </summary>
        public IList<TranslationEntity> Locales { get; set; }

        /// <summary>
        /// Gets the product attribute values
        /// </summary>
        public virtual ICollection<ProductAttributeValue> ProductAttributeValues
        {
            get { return _productAttributeValues ??= new List<ProductAttributeValue>(); }
            protected set { _productAttributeValues = value; }
        }

    }

}
