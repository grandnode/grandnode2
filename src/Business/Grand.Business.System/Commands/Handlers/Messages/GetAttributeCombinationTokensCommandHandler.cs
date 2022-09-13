using Grand.Business.Core.Commands.Messages;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.System.Commands.Handlers.Messages
{
    public class GetAttributeCombinationTokensCommandHandler : IRequestHandler<GetAttributeCombinationTokensCommand, LiquidAttributeCombination>
    {
        private readonly IProductAttributeFormatter _productAttributeFormatter;

        public GetAttributeCombinationTokensCommandHandler(
            IProductAttributeFormatter productAttributeFormatter)
        {
            _productAttributeFormatter = productAttributeFormatter;
        }

        public async Task<LiquidAttributeCombination> Handle(GetAttributeCombinationTokensCommand request, CancellationToken cancellationToken)
        {
            var liquidAttributeCombination = new LiquidAttributeCombination(request.Combination);
            liquidAttributeCombination.Formatted = await _productAttributeFormatter.FormatAttributes(request.Product, request.Combination.Attributes, null, renderPrices: false);
            liquidAttributeCombination.SKU = request.Product.FormatSku(request.Combination.Attributes);
            return liquidAttributeCombination;
        }
    }
}
