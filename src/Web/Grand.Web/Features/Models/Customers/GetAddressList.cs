using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Models.Customer;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Customers;

public class GetAddressList : IRequest<CustomerAddressListModel>
{
    public required Domain.Stores.Store Store { get; set; }
    public required Customer Customer { get; set; }
    public required Language Language { get; set; }
}