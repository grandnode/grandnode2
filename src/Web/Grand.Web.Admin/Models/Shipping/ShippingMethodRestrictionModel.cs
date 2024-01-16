using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;
using Grand.Web.Admin.Models.Directory;

namespace Grand.Web.Admin.Models.Shipping
{
    public class ShippingMethodRestrictionModel : BaseModel
    {
        public IList<ShippingMethodModel> AvailableShippingMethods { get; set; } = new List<ShippingMethodModel>();
        public IList<CountryModel> AvailableCountries { get; set; } = new List<CountryModel>();
        public IList<CustomerGroupModel> AvailableCustomerGroups { get; set; } = new List<CustomerGroupModel>();

        //[country id] / [shipping method id] / [restricted]
        public IDictionary<string, IDictionary<string, bool>> Restricted { get; set; } = new Dictionary<string, IDictionary<string, bool>>();
        public IDictionary<string, IDictionary<string, bool>> RestictedGroup { get; set; } = new Dictionary<string, IDictionary<string, bool>>();
    }
}