using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Web.Common.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Payments.OnAccount;

namespace Grand.Modules.Tests.Plugins.Payments;

[TestClass]
public class OnAccountPaymentProviderTests
{
    private const string CreditExceeded = "Plugins.Payments.OnAccount.CreditExceeded";

    private readonly Customer _buyer = new() { Id = "buyer-1", OwnerId = "owner-1" };
    private readonly Currency _workingCurrency = new() { CurrencyCode = "EUR" };
    private Mock<ICompanyCreditService> _creditService;
    private Mock<IShoppingCartSubTotalService> _subTotalService;
    private ServiceProvider _root;
    private IServiceScope _scope;
    private OnAccountPaymentSettings _settings;

    [TestInitialize]
    public void Init()
    {
        _settings = new OnAccountPaymentSettings();
        _creditService = new Mock<ICompanyCreditService>();
        _subTotalService = new Mock<IShoppingCartSubTotalService>();
        var customerService = new Mock<ICustomerService>();
        customerService.Setup(c => c.GetCustomerById(_buyer.Id)).ReturnsAsync(_buyer);
        var currencyService = new Mock<ICurrencyService>();
        //the working currency is worth half the primary one
        currencyService.Setup(c => c.ConvertToPrimaryStoreCurrency(It.IsAny<double>(), _workingCurrency))
            .ReturnsAsync((double amount, Currency _) => amount / 2);
        var translationService = new Mock<IRequestTranslationService>();
        translationService.Setup(t => t.GetResource(It.IsAny<string>())).Returns<string>(k => k);
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(c => c.WorkContext.CurrentCustomer).Returns(_buyer);
        contextAccessor.Setup(c => c.WorkContext.WorkingCurrency).Returns(_workingCurrency);
        contextAccessor.Setup(c => c.WorkContext.TaxDisplayType).Returns(TaxDisplayType.IncludingTax);
        contextAccessor.Setup(c => c.StoreContext.CurrentStore).Returns(new Grand.Domain.Stores.Store());

        //no IHttpContextAccessor: the provider has to work outside a request
        var services = new ServiceCollection();
        services.AddSingleton(_settings);
        services.AddSingleton(translationService.Object);
        services.AddSingleton(_creditService.Object);
        services.AddSingleton(customerService.Object);
        services.AddSingleton(_subTotalService.Object);
        services.AddSingleton(currencyService.Object);
        services.AddSingleton(Mock.Of<IPaymentTransactionService>());
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

    private void Credit(double? limit, double outstanding = 0, bool hasOverdue = false, int term = 14)
    {
        _creditService.Setup(c => c.GetCompanyCredit(_buyer))
            .ReturnsAsync(new CompanyCredit("owner-1", limit, limit.HasValue ? term : null, outstanding, hasOverdue));
    }

    private void CartSubTotal(double subTotalWithDiscount)
    {
        _subTotalService.Setup(c => c.GetShoppingCartSubTotal(It.IsAny<IList<ShoppingCartItem>>(),
                It.IsAny<CalculationContext>(), true))
            .ReturnsAsync(new ShoppingCartSubTotal(0d, [], subTotalWithDiscount + 10, subTotalWithDiscount,
                new SortedDictionary<double, double>()));
    }

    private Task<ProcessPaymentResult> Pay(double amount, double rate = 1)
    {
        return Provider.ProcessPayment(new PaymentTransaction {
            CustomerId = _buyer.Id, TransactionAmount = amount, CurrencyRate = rate
        });
    }

    [TestMethod]
    public void Provider_ResolvesFromContainerWithoutHttpContext()
    {
        Assert.IsInstanceOfType<OnAccountPaymentProvider>(Provider);
    }

    [TestMethod]
    public void PurchaseOrderNumberMode_ComesFromTheSettings()
    {
        _settings.PurchaseOrderNumberMode = PurchaseOrderNumberMode.Required;

        Assert.AreEqual(PurchaseOrderNumberMode.Required, Provider.PurchaseOrderNumberMode);
    }

    [TestMethod]
    public async Task ProcessPayment_CreditCovers_PendingWithDueDateAfterTerm()
    {
        Credit(1000, 400, term: 30);

        var result = await Pay(600);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(TransactionStatus.Pending, result.NewPaymentTransactionStatus);
        Assert.AreEqual(0, result.PaidAmount);
        Assert.AreEqual(DateTime.UtcNow.Date.AddDays(30), result.PaymentDueDateUtc);
    }

    [TestMethod]
    public async Task ProcessPayment_OverTheLimit_Refused()
    {
        //the method is hidden then, but its system name can still be posted
        Credit(1000, 900);

        var result = await Pay(100.01);

        CollectionAssert.Contains(result.Errors.ToList(), CreditExceeded);
        Assert.IsNull(result.PaymentDueDateUtc);
    }

    [TestMethod]
    public async Task ProcessPayment_OtherCurrency_CountsInPrimaryCurrency()
    {
        Credit(100);

        Assert.IsTrue((await Pay(400, 4)).Success);
        Assert.IsFalse((await Pay(400)).Success);
    }

    [TestMethod]
    public async Task ProcessPayment_Overdue_RefusedOnlyWhenBlocking()
    {
        Credit(1000, hasOverdue: true);

        Assert.IsFalse((await Pay(10)).Success);
        _settings.BlockWhenOverdue = false;
        Assert.IsTrue((await Pay(10)).Success);
    }

    [TestMethod]
    public async Task ProcessPayment_NoTradeCredit_Refused()
    {
        Credit(null);

        Assert.IsFalse((await Pay(10)).Success);
    }

    [TestMethod]
    public async Task ProcessPayment_UnknownCustomer_Refused()
    {
        var result = await Provider.ProcessPayment(new PaymentTransaction { CustomerId = "nobody", TransactionAmount = 1 });

        CollectionAssert.Contains(result.Errors.ToList(), CreditExceeded);
    }

    [TestMethod]
    public async Task HidePaymentMethod_NoTradeCredit_Hidden()
    {
        Credit(null);

        Assert.IsTrue(await Provider.HidePaymentMethod([]));
    }

    [TestMethod]
    public async Task HidePaymentMethod_SubTotalWithDiscountInPrimaryCurrencyDecides()
    {
        //200 in the working currency is 100 in the primary one
        Credit(1000, 900);
        CartSubTotal(200);
        Assert.IsFalse(await Provider.HidePaymentMethod([]));

        CartSubTotal(202);
        Assert.IsTrue(await Provider.HidePaymentMethod([]));
    }

    [TestMethod]
    public async Task HidePaymentMethod_Overdue_HiddenWhenBlocking()
    {
        Credit(1000, hasOverdue: true);
        CartSubTotal(10);

        Assert.IsTrue(await Provider.HidePaymentMethod([]));
    }
}
