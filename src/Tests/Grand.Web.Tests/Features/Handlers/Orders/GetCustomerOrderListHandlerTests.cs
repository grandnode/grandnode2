using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Mediator;
using Grand.Web.Common.Localization;
using Grand.Web.Features.Handlers.Orders;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.Orders;

[TestClass]
public class GetCustomerOrderListHandlerTests
{
    private Mock<ICompanyCreditService> _credit;
    private Currency _currency;
    private Customer _customer;
    private GetCustomerOrderListHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _customer = new Customer { Id = "buyer" };
        _currency = new Currency { Id = "c1", CurrencyCode = "EUR" };
        _credit = new Mock<ICompanyCreditService>();

        var orderService = new Mock<IOrderService>();
        orderService.Setup(o => o.SearchOrders(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<int?>(), It.IsAny<PaymentStatus?>(), It.IsAny<ShippingStatus?>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<Order>(new List<Order>(), 0, 10));
        var priceFormatter = new Mock<IPriceFormatter>();
        priceFormatter.Setup(p => p.FormatPrice(It.IsAny<double>(), _currency)).Returns<double, Currency>((p, _) => $"{p:0} EUR");
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(c => c.ConvertFromPrimaryStoreCurrency(It.IsAny<double>(), _currency))
            .ReturnsAsync((double v, Currency _) => v * 2);

        _handler = new GetCustomerOrderListHandler(new Mock<IDateTimeService>().Object,
            new Mock<IGroupService>().Object, new Mock<IMediator>().Object, priceFormatter.Object,
            new Mock<IOrderStatusService>().Object, orderService.Object, currencyService.Object, _credit.Object,
            new OrderSettings(), new Mock<IEnumTranslationService>().Object);
    }

    private Task<CustomerOrderListModel> Handle() =>
        _handler.Handle(new GetCustomerOrderList {
            Customer = _customer, Currency = _currency, Store = new Grand.Domain.Stores.Store { Id = "s1" }
        }, CancellationToken.None);

    [TestMethod]
    public async Task Handle_NoTradeCredit_LeavesTheSectionEmpty()
    {
        _credit.Setup(c => c.GetCompanyCredit(_customer)).ReturnsAsync(CompanyCredit.None("buyer"));

        var model = await Handle();

        Assert.IsNull(model.TradeCredit);
    }

    [TestMethod]
    public async Task Handle_TradeCredit_ShowsItInTheWorkingCurrency()
    {
        _credit.Setup(c => c.GetCompanyCredit(_customer))
            .ReturnsAsync(new CompanyCredit("buyer", 1000, 14, 400, true));

        var model = await Handle();

        Assert.AreEqual("2000 EUR", model.TradeCredit.CreditLimit);
        Assert.AreEqual("800 EUR", model.TradeCredit.Outstanding);
        Assert.AreEqual("1200 EUR", model.TradeCredit.Available);
        Assert.AreEqual(14, model.TradeCredit.PaymentTermDays);
        Assert.IsTrue(model.TradeCredit.HasOverdue);
    }
}
