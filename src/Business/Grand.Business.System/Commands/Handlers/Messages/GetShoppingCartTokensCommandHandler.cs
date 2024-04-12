using Grand.Business.Core.Commands.Messages.Tokens;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using MediatR;

namespace Grand.Business.System.Commands.Handlers.Messages;

public class GetShoppingCartTokensCommandHandler : IRequestHandler<GetShoppingCartTokensCommand, LiquidShoppingCart>
{
    private readonly IPictureService _pictureService;
    private readonly IProductAttributeFormatter _productAttributeFormatter;
    private readonly IProductService _productService;

    public GetShoppingCartTokensCommandHandler(
        IPictureService pictureService,
        IProductService productService,
        IProductAttributeFormatter productAttributeFormatter)
    {
        _pictureService = pictureService;
        _productService = productService;
        _productAttributeFormatter = productAttributeFormatter;
    }

    public async Task<LiquidShoppingCart> Handle(GetShoppingCartTokensCommand request,
        CancellationToken cancellationToken)
    {
        var liquidShoppingCart = new LiquidShoppingCart(request.Customer);
        await ProductList();

        async Task ProductList()
        {
            var attributeDescription = string.Empty;
            var pictureUrl = string.Empty;
            foreach (var item in request.Customer.ShoppingCartItems)
            {
                var product = await _productService.GetProductById(item.ProductId);

                if (product.ProductPictures.Any())
                    pictureUrl = await _pictureService.GetPictureUrl(
                        product.ProductPictures.MinBy(x => x.DisplayOrder)?.PictureId, 100,
                        storeLocation: request.Store.SslEnabled ? request.Store.SecureUrl : request.Store.Url);

                //attributes
                if (item.Attributes != null && item.Attributes.Any())
                    attributeDescription =
                        await _productAttributeFormatter.FormatAttributes(product, item.Attributes,
                            request.Customer);

                var result = new LiquidShoppingCartItem(product, attributeDescription, pictureUrl, item,
                    request.Language);


                liquidShoppingCart.Items.Add(result);
            }
        }

        return liquidShoppingCart;
    }
}