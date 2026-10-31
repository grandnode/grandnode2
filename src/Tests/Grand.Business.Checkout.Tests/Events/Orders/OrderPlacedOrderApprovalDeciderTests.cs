using Grand.Business.Checkout.Events.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Events.Orders;

[TestClass]
public class OrderPlacedOrderApprovalDeciderTests
{
    private Mock<ICustomerService> _customerService;
    private CustomerSettings _customerSettings;
    private OrderPlacedOrderApprovalHandler _handler;
    private OrderApproval _inserted;
    private Mock<ICustomerMessageProviderService> _messages;

    [TestInitialize]
    public void Init()
    {
        var approvalService = new Mock<IOrderApprovalService>();
        approvalService.Setup(x => x.Insert(It.IsAny<OrderApproval>())).Callback<OrderApproval>(a => _inserted = a)
            .Returns(Task.CompletedTask);
        _customerService = new Mock<ICustomerService>();
        _customerService.Setup(x => x.GetAllCustomers(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>(),
                It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<ShoppingCartType?>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Customer, object>>>(), It.IsAny<string[]>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<Customer>([], 0, int.MaxValue, 0));
        _customerService.Setup(x => x.GetCustomerById("owner"))
            .ReturnsAsync(new Customer { Id = "owner", Email = "owner@acme.example", Active = true });
        _messages = new Mock<ICustomerMessageProviderService>();

        var storeService = new Mock<IStoreService>();
        storeService.Setup(x => x.GetStoreById("store")).ReturnsAsync(new Store { Id = "store" });
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetCurrencyByCode("EUR")).ReturnsAsync(new Currency { CurrencyCode = "EUR" });

        _customerSettings = new CustomerSettings { HideSubAccountsTab = false };
        _handler = new OrderPlacedOrderApprovalHandler(approvalService.Object, _customerService.Object,
            _messages.Object, storeService.Object, new Mock<IProductService>().Object,
            new Mock<IPriceFormatter>().Object, currencyService.Object, new Mock<ISalesEmployeeService>().Object,
            _customerSettings, new LanguageSettings(), new Mock<ILogger<OrderPlacedOrderApprovalHandler>>().Object);
    }

    //the order's OwnerId holds the customer's own id when there is no owner, as PlaceOrderCommandHandler sets it
    private OrderPlacedEvent Placed(Customer buyer)
    {
        _customerService.Setup(x => x.GetCustomerById(buyer.Id)).ReturnsAsync(buyer);
        return new OrderPlacedEvent(new Order {
            Id = "order", StoreId = "store", CustomerId = buyer.Id, CustomerCurrencyCode = "EUR",
            OwnerId = string.IsNullOrEmpty(buyer.OwnerId) ? buyer.Id : buyer.OwnerId,
            OrderStatusId = (int)OrderStatusSystem.AwaitingApproval
        });
    }

    private void NoApproverIsAsked()
    {
        _messages.Verify(x => x.SendOrderApprovalRequestedMessage(It.IsAny<OrderApproval>(), It.IsAny<Customer>(),
            It.IsAny<Customer>(), It.IsAny<Store>(), It.IsAny<string>(), It.IsAny<DomainHost>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_RetailBuyer_StoreDecidesWithoutCompany()
    {
        await _handler.Handle(Placed(new Customer { Id = "retail", Email = "r@x.example" }), CancellationToken.None);

        Assert.AreEqual(OrderApprovalDecider.Store, _inserted.Decider);
        Assert.IsNull(_inserted.CompanyOwnerId);
        NoApproverIsAsked();
    }

    [TestMethod]
    public async Task Handle_CompanyOwnerBuyer_StoreDecides()
    {
        await _handler.Handle(Placed(new Customer { Id = "owner", Email = "owner@acme.example" }), CancellationToken.None);

        Assert.AreEqual(OrderApprovalDecider.Store, _inserted.Decider);
        Assert.IsNull(_inserted.CompanyOwnerId);
    }

    [TestMethod]
    public async Task Handle_SubAccount_CompanyDecides()
    {
        var buyer = new Customer { Id = "buyer", OwnerId = "owner", Email = "b@acme.example" };

        await _handler.Handle(Placed(buyer), CancellationToken.None);

        Assert.AreEqual(OrderApprovalDecider.Company, _inserted.Decider);
        Assert.AreEqual("owner", _inserted.CompanyOwnerId);
        _messages.Verify(x => x.SendOrderApprovalRequestedMessage(_inserted, buyer,
            It.Is<Customer>(c => c.Id == "owner"), It.IsAny<Store>(), It.IsAny<string>(), It.IsAny<DomainHost>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_SubAccountWithFeatureOff_StoreDecides()
    {
        _customerSettings.HideSubAccountsTab = true;

        await _handler.Handle(Placed(new Customer { Id = "buyer", OwnerId = "owner", Email = "b@acme.example" }),
            CancellationToken.None);

        Assert.AreEqual(OrderApprovalDecider.Store, _inserted.Decider);
        Assert.IsNull(_inserted.CompanyOwnerId);
        NoApproverIsAsked();
    }

    [TestMethod]
    public async Task Handle_RuleWantsTheStoreToDecide_StoreDecidesASubAccountsOrder()
    {
        var placed = Placed(new Customer { Id = "buyer", OwnerId = "owner", Email = "b@acme.example" });
        placed.ApprovalStoreDecides = true;

        await _handler.Handle(placed, CancellationToken.None);

        Assert.AreEqual(OrderApprovalDecider.Store, _inserted.Decider);
        Assert.IsNull(_inserted.CompanyOwnerId);
        NoApproverIsAsked();
    }

    [TestMethod]
    [DataRow(false, "owner@acme.example")]
    [DataRow(true, " ")]
    public async Task Handle_CompanyHasNobodyToAsk_StoreDecides(bool active, string email)
    {
        _customerService.Setup(x => x.GetCustomerById("owner"))
            .ReturnsAsync(new Customer { Id = "owner", Email = email, Active = active });

        await _handler.Handle(Placed(new Customer { Id = "buyer", OwnerId = "owner", Email = "b@acme.example" }),
            CancellationToken.None);

        Assert.AreEqual(OrderApprovalDecider.Store, _inserted.Decider);
        Assert.IsNull(_inserted.CompanyOwnerId);
        _messages.Verify(x => x.SendOrderApprovalStaffMessage(_inserted, It.IsAny<Customer>(), It.IsAny<Store>(),
            null, null, "Admin", It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_CopiesReasonsFromEvent()
    {
        var placed = Placed(new Customer { Id = "retail", Email = "r@x.example" });
        placed.ApprovalReasons = ["over the limit", "restricted goods"];

        await _handler.Handle(placed, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "over the limit", "restricted goods" }, _inserted.Reasons.ToArray());
    }
}
