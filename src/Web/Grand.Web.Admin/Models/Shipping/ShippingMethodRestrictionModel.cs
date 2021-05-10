using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;
using Grand.Web.Admin.Models.Directory;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Shipping
{
    public partial class ShippingMethodRestrictionModel : BaseModel
    {
        public ShippingMethodRestrictionModel()
        {
            AvailableShippingMethods = new List<ShippingMethodModel>();
            AvailableCountries = new List<CountryModel>();
            AvailableCustomerGroups = new List<CustomerGroupModel>();
            Restricted = new Dictionary<string, IDictionary<string, bool>>();
            RestictedGroup = new Dictionary<string, IDictionary<string, bool>>();
        }
        public IList<ShippingMethodModel> AvailableShippingMethods { get; set; }
        public IList<CountryModel> AvailableCountries { get; set; }
        public IList<CustomerGroupModel> AvailableCustomerGroups { get; set; }

        //[country id] / [shipping method id] / [restricted]
        public IDictionary<string, IDictionary<string, bool>> Restricted { get; set; }
        public IDictionary<string, IDictionary<string, bool>> RestictedGroup { get; set; }
    }
}