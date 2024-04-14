using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;

namespace Grand.Web.Vendor.Models.Catalog;

public class ProductAttributeLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Vendor.Catalog.Attributes.ProductAttributes.Fields.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Attributes.ProductAttributes.Fields.Description")]

    public string Description { get; set; }

    public string LanguageId { get; set; }
}

public class PredefinedProductAttributeValueModel : BaseEntityModel,
    ILocalizedModel<PredefinedProductAttributeValueLocalizedModel>
{
    public string ProductAttributeId { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.PriceAdjustment")]
    public double PriceAdjustment { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.PriceAdjustment")]
    //used only on the values list page
    public string PriceAdjustmentStr { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.WeightAdjustment")]
    public double WeightAdjustment { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.WeightAdjustment")]
    //used only on the values list page
    public string WeightAdjustmentStr { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.Cost")]
    public double Cost { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.IsPreSelected")]
    public bool IsPreSelected { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    public IList<PredefinedProductAttributeValueLocalizedModel> Locales { get; set; } =
        new List<PredefinedProductAttributeValueLocalizedModel>();
}

public class PredefinedProductAttributeValueLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Vendor.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.Name")]

    public string Name { get; set; }

    public string LanguageId { get; set; }
}