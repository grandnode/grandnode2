using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using MediatR;

namespace Grand.Business.Core.Commands.Messages
{
    public class GetShoppingCartTokensCommand : IRequest<LiquidShoppingCart>
    {
        public Customer Customer { get; set; }
        public Store Store { get; set; }
        public Language Language { get; set; }
    }
}
