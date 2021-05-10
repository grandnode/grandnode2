using Grand.Infrastructure.Models;
using Grand.Web.Admin.Models.Common;

namespace Grand.Web.Admin.Models.Orders
{
    public partial class OrderAddressModel : BaseModel
    {
        public string OrderId { get; set; }
        public bool BillingAddress { get; set; }
        public AddressModel Address { get; set; }
    }
}