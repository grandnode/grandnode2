using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Web.Models.Customer;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Customers;

public class GetAuctions : IRequest<CustomerAuctionsModel>
{
    public required Customer Customer { get; set; }
    public required Language Language { get; set; }
}