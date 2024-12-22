using Grand.Domain.Shipping;
using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Checkout;

public class CheckoutShippingMethodModel : BaseModel
{
    public IList<ShippingMethodModel> ShippingMethods { get; set; } = new List<ShippingMethodModel>();

    public string ShippingOption { get; set; }
    public IDictionary<string, string> Data { get; set; }
    public IList<string> Warnings { get; set; } = new List<string>();

    #region Nested classes

    public class ShippingMethodModel : BaseModel
    {
        public string ShippingRateProviderSystemName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Fee { get; set; }
        public bool Selected { get; set; }

        public ShippingOption ShippingOption { get; set; }
    }

    #endregion
}