using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using MediatR;

namespace Grand.Business.Core.Commands.Catalog
{
    public class SendOutBidCustomerCommand : IRequest<bool>
    {
        public Product Product { get; set; }
        public Bid Bid { get; set; }
        public Language Language { get; set; }
    }
}
