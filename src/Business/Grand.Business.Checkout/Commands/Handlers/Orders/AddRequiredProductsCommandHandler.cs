using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class AddRequiredProductsCommandHandler : IRequestHandler<AddRequiredProductsCommand, bool>
{
    private readonly IProductService _productService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly ShoppingCartSettings _shoppingCartSettings;

    public AddRequiredProductsCommandHandler(IShoppingCartService shoppingCartService,
        ShoppingCartSettings shoppingCartSettings
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

        if (!product.RequireOtherProducts) return true;
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
                var alreadyInTheCart = cart.Any(sci => sci.ProductId == rp.Id);
                //not in the cart
                if (alreadyInTheCart) continue;
                if (!product.AutoAddRequiredProducts) continue;
                var addToCart = await _shoppingCartService.AddToCart(customer,
                    rp.Id,
                    shoppingCartType,
                    storeId,
                    automaticallyAddRequiredProductsIfEnabled: false,
                    validator: new ShoppingCartValidatorOptions { GetRequiredProductWarnings = false });

                if (rp.RequireOtherProducts && addToCart.warnings.Count == 0)
                    await Handle(new AddRequiredProductsCommand {
                        Customer = customer,
                        ShoppingCartType = shoppingCartType,
                        StoreId = storeId,
                        Product = rp
                    }, cancellationToken);
            }
        }
        return true;
    }
}