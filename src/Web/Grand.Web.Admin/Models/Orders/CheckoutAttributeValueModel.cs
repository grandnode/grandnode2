using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;

namespace Grand.Web.Admin.Models.Orders;

public class CheckoutAttributeValueModel : BaseEntityModel, ILocalizedModel<CheckoutAttributeValueLocalizedModel>
{
    public string CheckoutAttributeId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Values.Fields.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Values.Fields.ColorSquaresRgb")]

    public string ColorSquaresRgb { get; set; }

    public bool DisplayColorSquaresRgb { get; set; }

    [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Values.Fields.PriceAdjustment")]
    public double PriceAdjustment { get; set; }

    public string PrimaryStoreCurrencyCode { get; set; }

    [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Values.Fields.WeightAdjustment")]
    public double WeightAdjustment { get; set; }

    public string BaseWeightIn { get; set; }

    [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Values.Fields.IsPreSelected")]
    public bool IsPreSelected { get; set; }

    [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Values.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    public IList<CheckoutAttributeValueLocalizedModel> Locales { get; set; } =
        new List<CheckoutAttributeValueLocalizedModel>();
}

public class CheckoutAttributeValueLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Values.Fields.Name")]

    public string Name { get; set; }

    public string LanguageId { get; set; }
}