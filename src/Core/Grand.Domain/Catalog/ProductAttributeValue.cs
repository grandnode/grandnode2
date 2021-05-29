using Grand.Domain.Localization;
using System.Collections.Generic;

namespace Grand.Domain.Catalog
{
    /// <summary>
    /// Represents a product attribute value
    /// </summary>
    public partial class ProductAttributeValue : SubBaseEntity, ITranslationEntity
    {
        public ProductAttributeValue()
        {
            Locales = new List<TranslationEntity>();
        }

        /// <summary>
        /// Gets or sets the attribute value type identifier
        /// </summary>
        public AttributeValueType AttributeValueTypeId { get; set; }

        /// <summary>
        /// Gets or sets the associated product identifier (used only with AttributeValueType.AssociatedToProduct)
        /// </summary>
        public string AssociatedProductId { get; set; }

        /// <summary>
        /// Gets or sets the product attribute name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the color RGB value (used with "Color squares" attribute type)
        /// </summary>
        public string ColorSquaresRgb { get; set; }

        /// <summary>
        /// Gets or sets the picture ID for image square (used with "Image squares" attribute type)
        /// </summary>
        public string ImageSquaresPictureId { get; set; }

        /// <summary>
        /// Gets or sets the price adjustment (used only with AttributeValueType.Simple)
        /// </summary>
        public double PriceAdjustment { get; set; }

        /// <summary>
        /// Gets or sets the weight adjustment (used only with AttributeValueType.Simple)
        /// </summary>
        public double WeightAdjustment { get; set; }

        /// <summary>
        /// Gets or sets the attibute value cost (used only with AttributeValueType.Simple)
        /// </summary>
        public double Cost { get; set; }

        /// <summary>
        /// Gets or sets the quantity of associated product (used only with AttributeValueType.AssociatedToProduct)
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the value is pre-selected
        /// </summary>
        public bool IsPreSelected { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the picture (identifier) associated with this value. This picture should replace a product main picture once clicked (selected).
        /// </summary>
        public string PictureId { get; set; }

        /// <summary>
        /// Gets or sets the collection of locales
        /// </summary>
        public IList<TranslationEntity> Locales { get; set; }
        
    }
}
