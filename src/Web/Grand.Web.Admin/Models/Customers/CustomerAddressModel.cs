using Grand.Infrastructure.Models;
using Grand.Web.Admin.Models.Common;

namespace Grand.Web.Admin.Models.Customers
{
    public partial class CustomerAddressModel : BaseModel
    {
        public string CustomerId { get; set; }

        public AddressModel Address { get; set; }
    }
}