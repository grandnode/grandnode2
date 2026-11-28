using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Directory;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Payments.BrainTree;
using Grand.Web.Common.Localization;
using System.Diagnostics.CodeAnalysis;

namespace Grand.Modules.Tests.Plugins.Payments;

[TestClass]
public class BrainTreePaymentProviderTests
{
    private Mock<ICurrencyService> _currencyService;
    private Mock<ICustomerService> _customerService;
    private HttpContextAccessor _httpContextAccessor;
    private ServiceProvider _root;
    private IServiceScope _scope;
    private BrainTreePaymentSettings _settings;
    private Mock<IShoppingCartSubTotalService> _subTotalService;

    public BrainTreePaymentProviderTests()
    {
        _settings = new BrainTreePaymentSettings();
        _subTotalService = new Mock<IShoppingCartSubTotalService>();
        _currencyService = new Mock<ICurrencyService>();
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(c => c.WorkContext.WorkingCurrency).Returns(new Currency());
        contextAccessor.Setup(c => c.StoreContext.CurrentStore).Returns(new Grand.Domain.Stores.Store());

        var services = new ServiceCollection();
        services.AddSingleton(_settings);
        services.AddSingleton(Mock.Of<ITranslationService>());
        services.AddSingleton(Mock.Of<IRequestTranslationService>());
        _customerService = new Mock<ICustomerService>();
        services.AddSingleton(_customerService.Object);
        //the card form lives in the session, but no request is running here: HttpContext is null
        _httpContextAccessor = new HttpContextAccessor();
        services.AddSingleton<IHttpContextAccessor>(_httpContextAccessor);
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
        _subTotalService.Setup(s => s.GetShoppingCartSubTotal(cart, It.IsAny<Grand.Business.Core.Utilities.Checkout.CalculationContext>(), true))
            .ReturnsAsync(new Grand.Business.Core.Utilities.Checkout.ShoppingCartSubTotal(0d, new List<ApplyDiscount>(),
                500d, 500d, new SortedDictionary<double, double>()));

        var provider = _scope.ServiceProvider.GetRequiredService<IPaymentProvider>();
        var fee = await provider.GetAdditionalHandlingFee(cart);

        Assert.AreEqual(10, fee, 0.0001);
        _currencyService.Verify(c => c.ConvertFromPrimaryStoreCurrency(It.IsAny<double>(), It.IsAny<Currency>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProcessPayment_RemovesCardDataFromSession_EvenWhenPaymentFails()
    {
        var session = new TestSession();
        _httpContextAccessor.HttpContext = new DefaultHttpContext { Session = session };
        _customerService.Setup(c => c.GetCustomerById("c1"))
            .ReturnsAsync(new Customer { BillingAddress = new Address() });
        var provider = _scope.ServiceProvider.GetRequiredService<IPaymentProvider>();
        await provider.SavePaymentInfo(new Dictionary<string, string> {
            { "CardholderName", "John" }, { "CardNumber", "4111111111111111" }, { "ExpireMonth", "12" },
            { "ExpireYear", "2030" }, { "CardCode", "123" }
        });
        Assert.IsNotEmpty(session.Keys);

        //no merchant is configured, so the gateway call fails
        await Assert.ThrowsAsync<Exception>(() =>
            provider.ProcessPayment(new PaymentTransaction { CustomerId = "c1", CurrencyCode = "USD" }));

        Assert.IsEmpty(session.Keys);
    }

    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _values = new();
        public bool IsAvailable => true;
        public string Id => "test";
        public IEnumerable<string> Keys => _values.Keys;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _values.TryGetValue(key, out value);
        public void Set(string key, byte[] value) => _values[key] = value;
        public void Remove(string key) => _values.Remove(key);
        public void Clear() => _values.Clear();
    }
}
