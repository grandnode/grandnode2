using Grand.Domain.Catalog;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Binders;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Models.Catalog;

public class ProductAttributeConditionModel : BaseModel, IProductValidVendor
{
    [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Condition.EnableCondition")]
    public bool EnableCondition { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Condition.Attributes")]
    public string SelectedProductAttributeId { get; set; }

    public IList<ProductAttributeModel> ProductAttributes { get; set; } = new List<ProductAttributeModel>();

    [ModelBinder(BinderType = typeof(CustomAttributesBinder))]
    public IList<CustomAttributeModel> SelectedAttributes { get; set; }

    public string ProductAttributeMappingId { get; set; }
    public string ProductId { get; set; }

    #region Nested classes

    public class ProductAttributeModel : BaseEntityModel
    {
        public string ProductAttributeId { get; set; }

        public string Name { get; set; }

        public string TextPrompt { get; set; }

        public bool IsRequired { get; set; }

        public AttributeControlType AttributeControlType { get; set; }

        public IList<ProductAttributeValueModel> Values { get; set; } = new List<ProductAttributeValueModel>();
    }

    public class ProductAttributeValueModel : BaseEntityModel
    {
        public string Name { get; set; }

        public bool IsPreSelected { get; set; }
    }

    #endregion
}