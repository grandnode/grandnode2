using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class AddRequiredProductsCommandHandler : IRequestHandler<AddRequiredProductsCommand, bool>
    {
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly IProductService _productService;

        public AddRequiredProductsCommandHandler(IShoppingCartService shoppingCartService, ShoppingCartSettings shoppingCartSettings
            , IProductService productService)
        {
            _shoppingCartService = shoppingCartService;
            _shoppingCartSettings = shoppingCartSettings;
            _productService = productService;
        }

        public async Task<bool> Handle(AddRequiredProductsCommand request, CancellationToken cancellationToken)
        {
            var customer = request.Customer;
            var shoppingCartType = request.ShoppingCartType;
            var storeId = request.StoreId;
            var product = request.Product;
            var cart = customer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartTypeId == shoppingCartType)
                .LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, storeId)
                .ToList();

            var warnings = new List<string>();

            if (product.RequireOtherProducts)
            {
                var requiredProducts = new List<Product>();
                foreach (var id in product.ParseRequiredProductIds())
                {
                    var rp = await _productService.GetProductById(id);
                    if (rp != null)
                        requiredProducts.Add(rp);
                }

                foreach (var rp in requiredProducts)
                {
                    //ensure that product is in the cart
                    bool alreadyInTheCart = false;
                    foreach (var sci in cart)
                    {
                        if (sci.ProductId == rp.Id)
                        {
                            alreadyInTheCart = true;
                            break;
                        }
                    }
                    //not in the cart
                    if (!alreadyInTheCart)
                    {

                        if (product.AutoAddRequiredProducts)
                        {
                            var addToCartWarnings = await _shoppingCartService.AddToCart(customer: customer,
                                productId: rp.Id,
                                shoppingCartType: shoppingCartType,
                                storeId: storeId,
                                automaticallyAddRequiredProductsIfEnabled: false, getRequiredProductWarnings: false);

                            if (rp.RequireOtherProducts && addToCartWarnings.Count == 0)
                            {
                                await Handle(new AddRequiredProductsCommand
                                {
                                    Customer = customer,
                                    ShoppingCartType = shoppingCartType,
                                    StoreId = storeId,
                                    Product = rp
                                }, cancellationToken);
                            }
                        }
                    }
                }
            }
            return true;
        }
    }

}