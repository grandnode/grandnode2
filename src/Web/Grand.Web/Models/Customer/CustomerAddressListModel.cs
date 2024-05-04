using Grand.Infrastructure.Models;
using Grand.Web.Models.Common;

namespace Grand.Web.Models.Customer;

public class CustomerAddressListModel : BaseModel
{
    public IList<AddressModel> Addresses { get; set; } = new List<AddressModel>();
}