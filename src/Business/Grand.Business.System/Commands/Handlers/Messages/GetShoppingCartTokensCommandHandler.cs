using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Extensions;
using Grand.Business.Messages.Commands.Models;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Business.Storage.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.System.Commands.Handlers.Messages
{
    public class GetShoppingCartTokensCommandHandler : IRequestHandler<GetShoppingCartTokensCommand, LiquidShoppingCart>
    {
        private readonly IPictureService _pictureService;
        private readonly IProductService _productService;
        private readonly IProductAttributeFormatter _productAttributeFormatter;

        public GetShoppingCartTokensCommandHandler(
            IPictureService pictureService,
            IProductService productService,
            IProductAttributeFormatter productAttributeFormatter)
        {
            _pictureService = pictureService;
            _productService = productService;
            _productAttributeFormatter = productAttributeFormatter;
        }

        public async Task<LiquidShoppingCart> Handle(GetShoppingCartTokensCommand request, CancellationToken cancellationToken)
        {
            var liquidShoppingCart = new LiquidShoppingCart(request.Customer);
            await ProductList();

            async Task ProductList()
            {
                string attributeDescription = string.Empty;
                string pictureUrl = string.Empty;
                foreach (var item in request.Customer.ShoppingCartItems)
                {
                    var product = await _productService.GetProductById(item.ProductId);
                    //product name
                    string productName = product.GetTranslation(x => x.Name, request.Language.Id);

                    if (product.ProductPictures.Any())
                    {
                        pictureUrl = await _pictureService.GetPictureUrl(product.ProductPictures.OrderBy(x => x.DisplayOrder).FirstOrDefault().PictureId, 100, storeLocation: request.Store.SslEnabled ? request.Store.SecureUrl : request.Store.Url);
                    }

                    //attributes
                    if (item.Attributes != null && item.Attributes.Any())
                    {
                        attributeDescription = await _productAttributeFormatter.FormatAttributes(product, item.Attributes, request.Customer);
                    }

                    var result = new LiquidShoppingCartItem(product, attributeDescription, pictureUrl, item, request.Language);


                    liquidShoppingCart.Items.Add(result);
                }
            }

            return liquidShoppingCart;
        }
    }
}
