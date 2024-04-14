using Grand.Infrastructure.Models;
using Grand.Web.Admin.Models.Directory;
using Grand.Web.Admin.Models.Shipping;

namespace Grand.Web.Admin.Models.Payments;

public class PaymentMethodRestrictionModel : BaseModel
{
    public IList<PaymentMethodModel> AvailablePaymentMethods { get; set; } = new List<PaymentMethodModel>();
    public IList<CountryModel> AvailableCountries { get; set; } = new List<CountryModel>();
    public IList<ShippingMethodModel> AvailableShippingMethods { get; set; } = new List<ShippingMethodModel>();

    //[payment method system name] / [resticted]
    public IDictionary<string, IDictionary<string, bool>> Resticted { get; set; } =
        new Dictionary<string, IDictionary<string, bool>>();

    public IDictionary<string, IDictionary<string, bool>> RestictedShipping { get; set; } =
        new Dictionary<string, IDictionary<string, bool>>();
}