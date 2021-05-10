using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;
using Grand.Web.Admin.Models.Directory;
using Grand.Web.Admin.Models.Shipping;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Payments
{
    public partial class PaymentMethodRestrictionModel : BaseModel
    {
        public PaymentMethodRestrictionModel()
        {
            AvailablePaymentMethods = new List<PaymentMethodModel>();
            AvailableCountries = new List<CountryModel>();
            AvailableShippingMethods = new List<ShippingMethodModel>();
            Resticted = new Dictionary<string, IDictionary<string, bool>>();
            RestictedShipping = new Dictionary<string, IDictionary<string, bool>>();
        }
        public IList<PaymentMethodModel> AvailablePaymentMethods { get; set; }
        public IList<CountryModel> AvailableCountries { get; set; }
        public IList<ShippingMethodModel> AvailableShippingMethods { get; set; }

        //[payment method system name] / [resticted]
        public IDictionary<string, IDictionary<string, bool>> Resticted { get; set; }
        public IDictionary<string, IDictionary<string, bool>> RestictedShipping { get; set; }
    }

}