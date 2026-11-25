using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Catalog;

public class SendOutBidCustomerCommand : IRequest<bool>
{
    public required Product Product { get; set; }
    public required Bid Bid { get; set; }
    public required Language Language { get; set; }
}