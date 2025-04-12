using Grand.Infrastructure.Models;
using Grand.Web.AdminShared.Models.Common;

namespace Grand.Web.AdminShared.Models.Customers;

public class CustomerAddressModel : BaseModel
{
    public string CustomerId { get; set; }

    public AddressModel Address { get; set; }
}