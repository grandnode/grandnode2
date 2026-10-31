using Grand.Business.Checkout.Events.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
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
public class OrderPlacedOrderApprovalStaffMailTests
{
    private readonly Customer _buyer = new() { Id = "retail", Email = "r@x.example" };
    private readonly Store _store = new() { Id = "store" };

    private Mock<ICustomerService> _customerService;
    private OrderPlacedOrderApprovalHandler _handler;
    private OrderApproval _inserted;
    private Mock<ILogger<OrderPlacedOrderApprovalHandler>> _logger;
    private Mock<ICustomerMessageProviderService> _messages;
    private Order _order;
    private Mock<ISalesEmployeeService> _salesEmployees;

    [TestInitialize]
    public void Init()
    {
        var approvalService = new Mock<IOrderApprovalService>();
        approvalService.Setup(x => x.Insert(It.IsAny<OrderApproval>())).Callback<OrderApproval>(a => _inserted = a)
            .Returns(Task.CompletedTask);
        _customerService = new Mock<ICustomerService>();
        _customerService.Setup(x => x.GetCustomerById("retail")).ReturnsAsync(_buyer);
        _messages = new Mock<ICustomerMessageProviderService>();
        _salesEmployees = new Mock<ISalesEmployeeService>();
        _logger = new Mock<ILogger<OrderPlacedOrderApprovalHandler>>();

        var storeService = new Mock<IStoreService>();
        storeService.Setup(x => x.GetStoreById("store")).ReturnsAsync(_store);
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetCurrencyByCode("EUR")).ReturnsAsync(new Currency { CurrencyCode = "EUR" });

        _handler = new OrderPlacedOrderApprovalHandler(approvalService.Object, _customerService.Object,
            _messages.Object, storeService.Object, new Mock<IProductService>().Object,
            new Mock<IPriceFormatter>().Object, currencyService.Object, _salesEmployees.Object,
            new CustomerSettings { HideSubAccountsTab = false },
            new LanguageSettings { DefaultAdminLanguageId = "admin-lang" }, _logger.Object);

        _order = new Order {
            Id = "order", StoreId = "store", CustomerId = "retail", OwnerId = "retail", CustomerCurrencyCode = "EUR",
            OrderStatusId = (int)OrderStatusSystem.AwaitingApproval
        };
    }

    private void SalesEmployee(bool active = true, string email = "sales@x.example")
    {
        _order.SeId = "se";
        _salesEmployees.Setup(x => x.GetSalesEmployeeById("se")).ReturnsAsync(new SalesEmployee {
            Id = "se", Name = "Sales", Email = email, Active = active
        });
    }

    private void Owner(bool active = true, bool deleted = false, string staffStoreId = null)
    {
        _store.OwnerCustomerId = "store-owner";
        _customerService.Setup(x => x.GetCustomerById("store-owner")).ReturnsAsync(new Customer {
            Id = "store-owner", Email = "owner@x.example", Active = active, Deleted = deleted,
            StaffStoreId = staffStoreId
        });
    }

    private void Mailed(string toEmail, string panelArea)
    {
        _messages.Verify(x => x.SendOrderApprovalStaffMessage(_inserted, _buyer, _store, toEmail, It.IsAny<string>(),
            panelArea, "admin-lang"), Times.Once);
    }

    private Task Place()
    {
        return _handler.Handle(new OrderPlacedEvent(_order), CancellationToken.None);
    }

    [TestMethod]
    public async Task Handle_StoreDecided_SalesEmployeeGetsTheMail()
    {
        SalesEmployee();
        Owner();

        await Place();

        Mailed("sales@x.example", "Admin");
    }

    [TestMethod]
    [DataRow(false, "sales@x.example")]
    [DataRow(true, "")]
    public async Task Handle_SalesEmployeeInactiveOrWithoutEmail_FallsToOwnerAccount(bool active, string email)
    {
        SalesEmployee(active, email);
        Owner();

        await Place();

        Mailed("owner@x.example", "Admin");
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, true)]
    public async Task Handle_OwnerAccountDeletedOrInactive_FallsToEmailAccount(bool active, bool deleted)
    {
        Owner(active, deleted);

        await Place();

        Mailed(null, "Admin");
    }

    [TestMethod]
    public async Task Handle_OwnerIsTheBuyer_FallsToEmailAccount()
    {
        //nobody decides their own order, so telling only them would leave the order with nobody aware of it
        _buyer.Active = true;
        _store.OwnerCustomerId = _buyer.Id;

        await Place();

        Mailed(null, "Admin");
    }

    [TestMethod]
    public async Task Handle_SalesEmployeeIsTheBuyer_FallsToOwnerAccount()
    {
        SalesEmployee(email: _buyer.Email);
        Owner();

        await Place();

        Mailed("owner@x.example", "Admin");
    }

    [TestMethod]
    public async Task Handle_NoSalesEmployeeAndNoOwner_FallsToEmailAccount()
    {
        await Place();

        Mailed(null, "Admin");
    }

    [TestMethod]
    public async Task Handle_OwnerWithStaffStore_LinksToStorePanel()
    {
        Owner(staffStoreId: "store");

        await Place();

        Mailed("owner@x.example", "Store");
    }

    [TestMethod]
    public async Task Handle_CompanyDecided_StoreIsNotMailed()
    {
        var buyer = new Customer { Id = "buyer", OwnerId = "owner", Email = "b@x.example" };
        _customerService.Setup(x => x.GetCustomerById("buyer")).ReturnsAsync(buyer);
        _customerService.Setup(x => x.GetCustomerById("owner")).ReturnsAsync((Customer)null);
        _order.CustomerId = "buyer";
        _order.OwnerId = "owner";

        await Place();

        _messages.Verify(x => x.SendOrderApprovalStaffMessage(It.IsAny<OrderApproval>(), It.IsAny<Customer>(),
            It.IsAny<Store>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_StaffMailFails_OrderEventStillCompletes()
    {
        _messages.Setup(x => x.SendOrderApprovalStaffMessage(It.IsAny<OrderApproval>(), It.IsAny<Customer>(),
                It.IsAny<Store>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("smtp"));

        await Place();

        Assert.IsNotNull(_inserted);
        _logger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<InvalidOperationException>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }
}
