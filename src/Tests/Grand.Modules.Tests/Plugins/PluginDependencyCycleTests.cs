using Grand.Business.Catalog.Services.Discounts;
using Grand.Business.Catalog.Services.Prices;
using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Checkout.Services.Payments;
using Grand.Business.Checkout.Services.Shipping;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Discounts;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Payments.BrainTree;
using Payments.CashOnDelivery;
using Shipping.ByWeight;
using Shipping.ByWeight.Services;
using Grand.Web.Common.Localization;

namespace Grand.Modules.Tests.Plugins;

/// <summary>
///     The real core services next to the real plugins, with only the leaves mocked:
///     a provider that depends back on the service that loads it fails ValidateOnBuild.
/// </summary>
[TestClass]
public class PluginDependencyCycleTests
{
    private static void AddMocks(IServiceCollection services, params Type[] types)
    {
        foreach (var type in types)
        {
            var mock = (Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(type))!;
            services.AddSingleton(type, mock.Object);
        }
    }

    private static ServiceProvider Build(IServiceCollection services)
    {
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [TestMethod]
    public void PaymentProviders_DoNotDependBackOnOrderCalculation()
    {
        var services = new ServiceCollection();
        services.AddScoped<IOrderCalculationService, OrderCalculationService>();
        services.AddScoped<IShoppingCartSubTotalService, ShoppingCartSubTotalService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPaymentProvider, CashOnDeliveryPaymentProvider>();
        services.AddScoped<IPaymentProvider, BrainTreePaymentProvider>();
        AddMocks(services, typeof(IContextAccessor), typeof(IPricingService), typeof(ITaxService),
            typeof(IShippingService), typeof(ICheckoutAttributeParser), typeof(IDiscountService),
            typeof(IGiftVoucherService), typeof(ILoyaltyPointsService), typeof(IProductService),
            typeof(ICurrencyService), typeof(IGroupService), typeof(IDiscountValidationService),
            typeof(ISettingService), typeof(ITranslationService), typeof(IRequestTranslationService), typeof(IPaymentTransactionService),
            typeof(ICustomerService), typeof(IHttpContextAccessor));
        services.AddSingleton(new TaxSettings());
        services.AddSingleton(new LoyaltyPointsSettings());
        services.AddSingleton(new ShippingSettings());
        services.AddSingleton(new ShoppingCartSettings());
        services.AddSingleton(new CatalogSettings());
        services.AddSingleton(new PaymentSettings());
        services.AddSingleton(new CashOnDeliveryPaymentSettings());
        services.AddSingleton(new BrainTreePaymentSettings());

        using var root = Build(services);
        using var scope = root.CreateScope();

        Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<IOrderCalculationService>());
    }

    [TestMethod]
    public void StandardDiscountRules_DoNotDependBackOnPricing()
    {
        var services = new ServiceCollection();
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<IDiscountHandlerService, DiscountHandlerService>();
        services.AddScoped<IDiscountValidationService, DiscountValidationService>();
        services.AddScoped<IDiscountProviderLoader, DiscountProviderLoader>();
        new global::DiscountRules.Standard.StartupApplication().ConfigureServices(services,
            new ConfigurationBuilder().Build());
        AddMocks(services, typeof(IContextAccessor), typeof(IProductService), typeof(IMediator),
            typeof(ICurrencyService), typeof(IDiscountService), typeof(ICategoryService), typeof(IBrandService),
            typeof(ICollectionService), typeof(IVendorService), typeof(IRepository<DiscountCoupon>),
            typeof(IRepository<DiscountUsageHistory>), typeof(IOrderService), typeof(ITranslationService),
            typeof(IRequestTranslationService));
        services.AddSingleton(new ShoppingCartSettings());
        services.AddSingleton(new CatalogSettings());

        using var root = Build(services);
        using var scope = root.CreateScope();

        Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<IPricingService>());
    }

    [TestMethod]
    public void ShippingProviders_DoNotDependBackOnShipping()
    {
        //ShippingService builds every provider eagerly; ByWeight pulls in pricing, and pricing every discount rule
        var services = new ServiceCollection();
        services.AddScoped<IShippingService, ShippingService>();
        services.AddScoped<IShippingRateCalculationProvider, ByWeightShippingCalcPlugin>();
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<IDiscountHandlerService, DiscountHandlerService>();
        services.AddScoped<IDiscountValidationService, DiscountValidationService>();
        services.AddScoped<IDiscountProviderLoader, DiscountProviderLoader>();
        new global::DiscountRules.Standard.StartupApplication().ConfigureServices(services,
            new ConfigurationBuilder().Build());
        AddMocks(services, typeof(ILogger<ShippingService>), typeof(ICountryService),
            typeof(IShippingMethodService), typeof(IShippingByWeightService), typeof(ICheckoutAttributeParser),
            typeof(IContextAccessor), typeof(IProductService), typeof(IMediator), typeof(ICurrencyService),
            typeof(IDiscountService), typeof(ICategoryService), typeof(IBrandService), typeof(ICollectionService),
            typeof(IVendorService), typeof(IRepository<DiscountCoupon>), typeof(IRepository<DiscountUsageHistory>),
            typeof(IOrderService),
            typeof(ITranslationService), typeof(IRequestTranslationService));
        services.AddSingleton(new ShippingProviderSettings());
        services.AddSingleton(new ShippingSettings());
        services.AddSingleton(new ByWeightShippingSettings());
        services.AddSingleton(new ShoppingCartSettings());
        services.AddSingleton(new CatalogSettings());

        using var root = Build(services);
        using var scope = root.CreateScope();

        Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<IShippingService>());
    }
}
