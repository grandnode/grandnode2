using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Payments.CashOnDelivery;

namespace Grand.Modules.Tests.Plugins.Payments;

[TestClass]
public class CashOnDeliveryPaymentProviderTests
{
    private readonly Currency _workingCurrency = new() { CurrencyCode = "EUR" };
    private Mock<ICurrencyService> _currencyService;
    private Mock<IPaymentTransactionService> _paymentTransactionService;
    private ServiceProvider _root;
    private IServiceScope _scope;
    private CashOnDeliveryPaymentSettings _settings;
    private Mock<IShoppingCartSubTotalService> _subTotalService;

    [TestInitialize]
    public void Init()
    {
        _settings = new CashOnDeliveryPaymentSettings();
        _subTotalService = new Mock<IShoppingCartSubTotalService>();
        _currencyService = new Mock<ICurrencyService>();
        _paymentTransactionService = new Mock<IPaymentTransactionService>();
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(c => c.WorkContext.WorkingCurrency).Returns(_workingCurrency);

        //no IHttpContextAccessor: the provider has to work outside a request
        var services = new ServiceCollection();
        services.AddSingleton(_settings);
        services.AddSingleton(Mock.Of<ITranslationService>());
        services.AddSingleton(_subTotalService.Object);
        services.AddSingleton(_currencyService.Object);
        services.AddSingleton(_paymentTransactionService.Object);
        services.AddSingleton(contextAccessor.Object);
        new StartupApplication().ConfigureServices(services, new ConfigurationBuilder().Build());

        _root = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        _scope = _root.CreateScope();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _scope.Dispose();
        _root.Dispose();
    }

    private IPaymentProvider Provider => _scope.ServiceProvider.GetRequiredService<IPaymentProvider>();

    [TestMethod]
    public void Provider_ResolvesFromContainerWithoutHttpContext()
    {
        Assert.IsInstanceOfType<CashOnDeliveryPaymentProvider>(Provider);
    }

    [TestMethod]
    public async Task GetAdditionalHandlingFee_Percentage_IsShareOfSubtotalWithoutSecondConversion()
    {
        _settings.AdditionalFee = 10;
        _settings.AdditionalFeePercentage = true;
        var cart = new List<ShoppingCartItem> { new() };
        _subTotalService.Setup(s => s.GetShoppingCartSubTotal(cart, true))
            .ReturnsAsync((0d, new List<ApplyDiscount>(), 200d, 180d, new SortedDictionary<double, double>()));

        var fee = await Provider.GetAdditionalHandlingFee(cart);

        Assert.AreEqual(18, fee, 0.0001);
        //the subtotal is already in the working currency
        _currencyService.Verify(c => c.ConvertFromPrimaryStoreCurrency(It.IsAny<double>(), It.IsAny<Currency>()),
            Times.Never);
    }

    [TestMethod]
    public async Task GetAdditionalHandlingFee_FixedValue_IsConvertedToWorkingCurrency()
    {
        _settings.AdditionalFee = 5;
        _currencyService.Setup(c => c.ConvertFromPrimaryStoreCurrency(5, _workingCurrency)).ReturnsAsync(20);

        var fee = await Provider.GetAdditionalHandlingFee(new List<ShoppingCartItem>());

        Assert.AreEqual(20, fee);
        _subTotalService.Verify(s => s.GetShoppingCartSubTotal(It.IsAny<IList<ShoppingCartItem>>(), It.IsAny<bool>()),
            Times.Never);
    }

    [TestMethod]
    public async Task CancelPayment_SavesTransactionAsCanceled()
    {
        var transaction = new PaymentTransaction { TransactionStatus = TransactionStatus.Pending };

        await Provider.CancelPayment(transaction);

        Assert.AreEqual(TransactionStatus.Canceled, transaction.TransactionStatus);
        _paymentTransactionService.Verify(s => s.UpdatePaymentTransaction(transaction), Times.Once);
    }
}
