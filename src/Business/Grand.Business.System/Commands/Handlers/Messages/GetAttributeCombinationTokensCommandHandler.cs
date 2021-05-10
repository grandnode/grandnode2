using Grand.Business.Catalog.Extensions;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Messages.Commands.Models;
using Grand.Business.Messages.DotLiquidDrops;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.System.Commands.Handlers.Messages
{
    public class GetAttributeCombinationTokensCommandHandler : IRequestHandler<GetAttributeCombinationTokensCommand, LiquidAttributeCombination>
    {
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly IProductAttributeParser _productAttributeParser;

        public GetAttributeCombinationTokensCommandHandler(
            IProductAttributeFormatter productAttributeFormatter,
            IProductAttributeParser productAttributeParser)
        {
            _productAttributeFormatter = productAttributeFormatter;
            _productAttributeParser = productAttributeParser;
        }

        public async Task<LiquidAttributeCombination> Handle(GetAttributeCombinationTokensCommand request, CancellationToken cancellationToken)
        {
            var liquidAttributeCombination = new LiquidAttributeCombination(request.Combination);
            liquidAttributeCombination.Formatted = await _productAttributeFormatter.FormatAttributes(request.Product, request.Combination.Attributes, null, renderPrices: false);
            liquidAttributeCombination.SKU = request.Product.FormatSku(request.Combination.Attributes, _productAttributeParser);
            return liquidAttributeCombination;
        }
    }
}
