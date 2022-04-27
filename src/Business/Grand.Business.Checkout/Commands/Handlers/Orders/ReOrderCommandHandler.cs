using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Tax;
using MediatR;
using Grand.Business.Core.Utilities.Checkout;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class ReOrderCommandHandler : IRequestHandler<ReOrderCommand, IList<string>>
    {
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly TaxSettings _taxSettings;

        public ReOrderCommandHandler(
            ICustomerService customerService,
            IProductService productService,
            IShoppingCartService shoppingCartService,
            TaxSettings taxSettings)
        {
            _customerService = customerService;
            _productService = productService;
            _shoppingCartService = shoppingCartService;
            _taxSettings = taxSettings;
        }

        public async Task<IList<string>> Handle(ReOrderCommand request, CancellationToken cancellationToken)
        {
            if (request.Order == null)
                throw new ArgumentNullException(nameof(request.Order));

            var warnings = new List<string>();
            var customer = await _customerService.GetCustomerById(request.Order.CustomerId);

            foreach (var orderItem in request.Order.OrderItems)
            {
                var product = await _productService.GetProductById(orderItem.ProductId);
                if (product != null)
                {
                    if (product.ProductTypeId == ProductType.SimpleProduct)
                    {
                        warnings.AddRange((await _shoppingCartService.AddToCart(customer, orderItem.ProductId,
                            ShoppingCartType.ShoppingCart, request.Order.StoreId, orderItem.WarehouseId,
                            orderItem.Attributes,
                            product.EnteredPrice ?
                            _taxSettings.PricesIncludeTax ? orderItem.UnitPriceInclTax : orderItem.UnitPriceExclTax
                            : (double?)default,
                            orderItem.RentalStartDateUtc, orderItem.RentalEndDateUtc,
                            orderItem.Quantity, false,
                            validator: new ShoppingCartValidatorOptions() { GetRequiredProductWarnings = false })).warnings);
                    }
                }
                else
                {
                    warnings.Add("Product is not available");
                }
            }

            return warnings;
        }
    }
}
