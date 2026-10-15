using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Payments.BrainTree;

namespace Grand.Modules.Tests.Plugins.Payments;

[TestClass]
public class BrainTreePaymentProviderTests
{
    private Mock<ICurrencyService> _currencyService;
    private ServiceProvider _root;
    private IServiceScope _scope;
    private BrainTreePaymentSettings _settings;
    private Mock<IShoppingCartSubTotalService> _subTotalService;

    [TestInitialize]
    public void Init()
    {
        _settings = new BrainTreePaymentSettings();
        _subTotalService = new Mock<IShoppingCartSubTotalService>();
        _currencyService = new Mock<ICurrencyService>();
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(c => c.WorkContext.WorkingCurrency).Returns(new Currency());

        var services = new ServiceCollection();
        services.AddSingleton(_settings);
        services.AddSingleton(Mock.Of<ITranslationService>());
        services.AddSingleton(Mock.Of<ICustomerService>());
        //the card form lives in the session, but no request is running here: HttpContext is null
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        services.AddSingleton(_subTotalService.Object);
        services.AddSingleton(_currencyService.Object);
        services.AddSingleton(contextAccessor.Object);
        services.AddScoped<IPaymentProvider, BrainTreePaymentProvider>();

        _root = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        _scope = _root.CreateScope();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _scope.Dispose();
        _root.Dispose();
    }

    [TestMethod]
    public async Task GetAdditionalHandlingFee_Percentage_WorksWithoutHttpContext()
    {
        _settings.AdditionalFee = 2;
        _settings.AdditionalFeePercentage = true;
        var cart = new List<ShoppingCartItem> { new() };
        _subTotalService.Setup(s => s.GetShoppingCartSubTotal(cart, true))
            .ReturnsAsync((0d, new List<ApplyDiscount>(), 500d, 500d, new SortedDictionary<double, double>()));

        var provider = _scope.ServiceProvider.GetRequiredService<IPaymentProvider>();
        var fee = await provider.GetAdditionalHandlingFee(cart);

        Assert.AreEqual(10, fee, 0.0001);
        _currencyService.Verify(c => c.ConvertFromPrimaryStoreCurrency(It.IsAny<double>(), It.IsAny<Currency>()),
            Times.Never);
    }
}
