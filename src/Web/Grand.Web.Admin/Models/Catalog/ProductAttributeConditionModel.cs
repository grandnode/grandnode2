﻿using Grand.Domain.Catalog;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Catalog
{
    public partial class ProductAttributeConditionModel : BaseModel
    {
        public ProductAttributeConditionModel()
        {
            ProductAttributes = new List<ProductAttributeModel>();
        }

        [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Condition.EnableCondition")]
        public bool EnableCondition { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Condition.Attributes")]
        public string SelectedProductAttributeId { get; set; }
        public IList<ProductAttributeModel> ProductAttributes { get; set; }

        public string ProductAttributeMappingId { get; set; }
        public string ProductId { get; set; }

        #region Nested classes

        public partial class ProductAttributeModel : BaseEntityModel
        {
            public ProductAttributeModel()
            {
                Values = new List<ProductAttributeValueModel>();
            }

            public string ProductAttributeId { get; set; }

            public string Name { get; set; }

            public string TextPrompt { get; set; }

            public bool IsRequired { get; set; }

            public AttributeControlType AttributeControlType { get; set; }

            public IList<ProductAttributeValueModel> Values { get; set; }
        }

        public partial class ProductAttributeValueModel : BaseEntityModel
        {
            public string Name { get; set; }

            public bool IsPreSelected { get; set; }
        }
        #endregion
    }
}