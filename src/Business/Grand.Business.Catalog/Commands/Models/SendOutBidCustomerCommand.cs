using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using MediatR;

namespace Grand.Business.Catalog.Commands.Models
{
    public class SendOutBidCustomerCommand : IRequest<bool>
    {
        public Product Product { get; set; }
        public Bid Bid { get; set; }
        public Language Language { get; set; }
    }
}
