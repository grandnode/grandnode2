using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using MediatR;

namespace Grand.Business.Core.Commands.Messages
{
    public class GetOrderTokensCommand : IRequest<LiquidOrder>
    {
        public Order Order { get; set; }
        public Customer Customer { get; set; }
        public Store Store { get; set; }
        public DomainHost Host { get; set; }
        public OrderNote OrderNote { get; set; } = null;
        public Vendor Vendor { get; set; } = null;
        public double RefundedAmount { get; set; } = 0;
    }
}
