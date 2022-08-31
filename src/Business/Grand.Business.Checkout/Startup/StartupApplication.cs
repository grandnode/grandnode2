using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Checkout.Services.CheckoutAttributes;
using Grand.Business.Checkout.Services.GiftVouchers;
using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Checkout.Services.Payments;
using Grand.Business.Checkout.Services.Shipping;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Grand.Business.Core.Utilities.Checkout;

namespace Grand.Business.Checkout.Startup
{
    public class StartupApplication : IStartupApplication
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            RegisterOrdersService(services);
            RegisterPaymentsService(services);
            RegisterShippingService(services);
        }
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {

        }
        public int Priority => 100;
        public bool BeforeConfigure => false;


        private void RegisterOrdersService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ILoyaltyPointsService, LoyaltyPointsService>();
            serviceCollection.AddScoped<IGiftVoucherService, GiftVoucherService>();
            serviceCollection.AddScoped<IOrderService, OrderService>();
            serviceCollection.AddScoped<IOrderStatusService, OrderStatusService>();
            serviceCollection.AddScoped<IOrderCalculationService, OrderCalculationService>();
            serviceCollection.AddScoped<IMerchandiseReturnService, MerchandiseReturnService>();
            serviceCollection.AddScoped<ILoyaltyPointsService, LoyaltyPointsService>();
            serviceCollection.AddScoped<IShoppingCartService, ShoppingCartService>();
            serviceCollection.AddScoped<IShoppingCartValidator, ShoppingCartValidator>();
            serviceCollection.AddScoped<ICheckoutAttributeFormatter, CheckoutAttributeFormatter>();
            serviceCollection.AddScoped<ICheckoutAttributeParser, CheckoutAttributeParser>();
            serviceCollection.AddScoped<ICheckoutAttributeService, CheckoutAttributeService>();
            serviceCollection.AddScoped<IOrderTagService, OrderTagService>();

        }
        private void RegisterPaymentsService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IPaymentService, PaymentService>();
            serviceCollection.AddScoped<IPaymentTransactionService, PaymentTransactionService>();
        }
        private void RegisterShippingService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IShipmentService, ShipmentService>();
            serviceCollection.AddScoped<IShippingService, ShippingService>();
            serviceCollection.AddScoped<IPickupPointService, PickupPointService>();
            serviceCollection.AddScoped<IDeliveryDateService, DeliveryDateService>();
            serviceCollection.AddScoped<IWarehouseService, WarehouseService>();
            serviceCollection.AddScoped<IShippingMethodService, ShippingMethodService>();
        }
    }
}
