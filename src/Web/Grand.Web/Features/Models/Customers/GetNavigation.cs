using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Models.Customer;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Customers;

public class GetNavigation : IRequest<CustomerNavigationModel>
{
    public int SelectedTabId { get; set; }
    public required Customer Customer { get; set; }
    public required Language Language { get; set; }
    public required Domain.Stores.Store Store { get; set; }
    public Domain.Vendors.Vendor? Vendor { get; set; }
}