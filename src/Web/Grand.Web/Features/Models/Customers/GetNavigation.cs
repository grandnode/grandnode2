using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Models.Customer;
using MediatR;

namespace Grand.Web.Features.Models.Customers;

public class GetNavigation : IRequest<CustomerNavigationModel>
{
    public int SelectedTabId { get; set; }
    public Customer Customer { get; set; }
    public Language Language { get; set; }
    public Store Store { get; set; }
    public Domain.Vendors.Vendor Vendor { get; set; }
}