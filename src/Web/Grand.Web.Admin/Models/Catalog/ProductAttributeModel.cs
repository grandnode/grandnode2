using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Catalog;

public class ProductAttributeModel : BaseEntityModel, ILocalizedModel<ProductAttributeLocalizedModel>, IStoreLinkModel
{
    [GrandResourceDisplayName("Admin.Catalog.Attributes.ProductAttributes.Fields.Name")]
    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Attributes.ProductAttributes.Fields.SeName")]
    public string SeName { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Attributes.ProductAttributes.Fields.Description")]
    public string Description { get; set; }

    public IList<ProductAttributeLocalizedModel> Locales { get; set; } = new List<ProductAttributeLocalizedModel>();

    //Store acl
    [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.LimitedToStores")]
    [UIHint("Stores")]
    public string[] Stores { get; set; }

    #region Nested classes

    public class UsedByProductModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Catalog.Attributes.ProductAttributes.UsedByProducts.Product")]
        public string ProductName { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Attributes.ProductAttributes.UsedByProducts.Published")]
        public bool Published { get; set; }
    }

    #endregion
}

public class ProductAttributeLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Admin.Catalog.Attributes.ProductAttributes.Fields.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Attributes.ProductAttributes.Fields.Description")]

    public string Description { get; set; }

    public string LanguageId { get; set; }
}

public class PredefinedProductAttributeValueModel : BaseEntityModel,
    ILocalizedModel<PredefinedProductAttributeValueLocalizedModel>
{
    public string ProductAttributeId { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.PriceAdjustment")]
    public double PriceAdjustment { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.PriceAdjustment")]
    //used only on the values list page
    public string PriceAdjustmentStr { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.WeightAdjustment")]
    public double WeightAdjustment { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.WeightAdjustment")]
    //used only on the values list page
    public string WeightAdjustmentStr { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.Cost")]
    public double Cost { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.IsPreSelected")]
    public bool IsPreSelected { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    public IList<PredefinedProductAttributeValueLocalizedModel> Locales { get; set; } =
        new List<PredefinedProductAttributeValueLocalizedModel>();
}

public class PredefinedProductAttributeValueLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Admin.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.Name")]

    public string Name { get; set; }

    public string LanguageId { get; set; }
}