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
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Events.Orders;

[TestClass]
public class OrderPlacedOrderApprovalHandlerTests
{
    private Mock<IOrderApprovalService> _approvalService;
    private Customer _buyer;
    private Mock<ICustomerService> _customerService;
    private OrderPlacedOrderApprovalHandler _handler;
    private Mock<ICustomerMessageProviderService> _messages;
    private Order _order;
    private Mock<ILogger<OrderPlacedOrderApprovalHandler>> _logger;
    private Customer _owner;
    private OrderApproval _inserted;

    [TestInitialize]
    public void Init()
    {
        _approvalService = new Mock<IOrderApprovalService>();
        _approvalService.Setup(x => x.Insert(It.IsAny<OrderApproval>())).Callback<OrderApproval>(a => _inserted = a)
            .Returns(Task.CompletedTask);
        _customerService = new Mock<ICustomerService>();
        _messages = new Mock<ICustomerMessageProviderService>();

        var store = new Store { Id = "store" };
        var storeService = new Mock<IStoreService>();
        storeService.Setup(x => x.GetStoreById("store")).ReturnsAsync(store);

        var product = new Product { Id = "p1", Name = "Chair" };
        var productService = new Mock<IProductService>();
        productService.Setup(x => x.GetProductById("p1", false)).ReturnsAsync(product);

        var currency = new Currency { CurrencyCode = "EUR" };
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetCurrencyByCode("EUR")).ReturnsAsync(currency);

        var priceFormatter = new Mock<IPriceFormatter>();
        priceFormatter.Setup(x => x.FormatPrice(It.IsAny<double>(), currency))
            .Returns<double, Currency>((p, _) => $"{p:0.00} EUR");

        _logger = new Mock<ILogger<OrderPlacedOrderApprovalHandler>>();
        _handler = new OrderPlacedOrderApprovalHandler(_approvalService.Object, _customerService.Object,
            _messages.Object, storeService.Object, productService.Object, priceFormatter.Object,
            currencyService.Object, _logger.Object);

        _owner = new Customer { Id = "owner", Email = "owner@acme.example", Active = true };
        _buyer = new Customer {
            Id = "buyer", OwnerId = "owner", Email = "buyer@acme.example", Active = true,
            SubAccountRole = SubAccountRole.Buyer
        };
        _customerService.Setup(x => x.GetCustomerById("buyer")).ReturnsAsync(_buyer);
        _customerService.Setup(x => x.GetCustomerById("owner")).ReturnsAsync(_owner);

        _order = new Order {
            Id = "order", StoreId = "store", CustomerId = "buyer", OwnerId = "owner", OrderTotal = 500,
            CustomerCurrencyCode = "EUR", CustomerLanguageId = "en",
            OrderStatusId = (int)OrderStatusSystem.AwaitingApproval,
            CustomerTaxDisplayTypeId = Grand.Domain.Tax.TaxDisplayType.IncludingTax,
            OrderSubtotalInclTax = 450, OrderSubTotalDiscountInclTax = 10, OrderShippingInclTax = 40,
            OrderTax = 20, ShippingMethod = "Courier"
        };
        _order.OrderItems.Add(new OrderItem {
            ProductId = "p1", Sku = "CH-1", Quantity = 2, UnitPriceInclTax = 225, PriceInclTax = 450,
            AttributeDescription = "Color: red<br />Size: L"
        });
    }

    private void Staff(params Customer[] staff)
    {
        _customerService.Setup(x => x.GetAllCustomers(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(),
                It.IsAny<string>(), "store", "owner", It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<string[]>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<ShoppingCartType?>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<System.Linq.Expressions.Expression<Func<Customer, object>>>(),
                It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<Customer>(staff.ToList(), 0, int.MaxValue, staff.Length));
    }

    private static Customer Approver(string id, bool active = true, bool deleted = false, string email = null)
    {
        return new Customer {
            Id = id, OwnerId = "owner", SubAccountRole = SubAccountRole.Approver, Active = active, Deleted = deleted,
            Email = email ?? $"{id}@acme.example"
        };
    }

    [TestMethod]
    public async Task Handle_OrderNotAwaitingApproval_DoesNothing()
    {
        _order.OrderStatusId = (int)OrderStatusSystem.Pending;

        await _handler.Handle(new OrderPlacedEvent(_order), CancellationToken.None);

        _approvalService.Verify(x => x.Insert(It.IsAny<OrderApproval>()), Times.Never);
        _messages.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Handle_AwaitingOrder_RecordsThePendingRequestFromTheOrder()
    {
        Staff();

        await _handler.Handle(new OrderPlacedEvent(_order), CancellationToken.None);

        Assert.IsNotNull(_inserted);
        Assert.AreEqual(OrderApprovalStatus.Pending, _inserted.Status);
        Assert.AreEqual("order", _inserted.OrderId);
        Assert.AreEqual("owner", _inserted.CompanyOwnerId);
        Assert.AreEqual("buyer", _inserted.CustomerId);
        Assert.AreEqual("store", _inserted.StoreId);
        Assert.AreEqual(500, _inserted.Amount);
        Assert.AreEqual("EUR", _inserted.CurrencyCode);
        Assert.HasCount(1, _inserted.Items);
        var item = _inserted.Items[0];
        Assert.AreEqual("Chair", item.ProductName);
        Assert.AreEqual("CH-1", item.Sku);
        Assert.AreEqual(2, item.Quantity);
        Assert.AreEqual("Color: red, Size: L", item.AttributeDescription);
        Assert.AreEqual("225.00 EUR", item.UnitPrice);
        Assert.AreEqual("450.00 EUR", item.LineTotal);
        Assert.AreEqual("450.00 EUR", _inserted.Summary.SubTotal);
        Assert.AreEqual("10.00 EUR", _inserted.Summary.Discount);
        Assert.AreEqual("Courier", _inserted.Summary.ShippingMethod);
        Assert.AreEqual("40.00 EUR", _inserted.Summary.Shipping);
        Assert.AreEqual("20.00 EUR", _inserted.Summary.Tax);
        Assert.AreEqual("500.00 EUR", _inserted.Summary.Total);
    }

    [TestMethod]
    public async Task Handle_AlreadyRecorded_DoesNotInsertAgain()
    {
        Staff();
        _approvalService.Setup(x => x.GetByOrderId("order")).ReturnsAsync(new OrderApproval());

        await _handler.Handle(new OrderPlacedEvent(_order), CancellationToken.None);

        _approvalService.Verify(x => x.Insert(It.IsAny<OrderApproval>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_SendsToTheOwnerAndActiveApproversOnly()
    {
        var buyerApprover = Approver("buyer");
        var goodApprover = Approver("a1");
        Staff(buyerApprover, goodApprover, Approver("inactive", false), Approver("deleted", deleted: true),
            Approver("noemail", email: " "), new Customer {
                Id = "plain", OwnerId = "owner", SubAccountRole = SubAccountRole.Buyer, Active = true,
                Email = "plain@acme.example"
            });
        _buyer.SubAccountRole = SubAccountRole.Approver;

        await _handler.Handle(new OrderPlacedEvent(_order), CancellationToken.None);

        var recipients = new List<Customer>();
        _messages.Verify(x => x.SendOrderApprovalRequestedMessage(_inserted, _buyer, Capture.In(recipients),
            It.IsAny<Store>(), "en", null), Times.Exactly(2));
        CollectionAssert.AreEquivalent(new[] { "owner", "a1" }, recipients.Select(x => x.Id).ToList());
    }

    [TestMethod]
    public async Task Handle_InsertThrows_CompletesAndLogs()
    {
        Staff();
        _approvalService.Setup(x => x.Insert(It.IsAny<OrderApproval>())).ThrowsAsync(new InvalidOperationException("db"));

        await _handler.Handle(new OrderPlacedEvent(_order), CancellationToken.None);

        _logger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_BuyerMissing_CompletesAndLogs()
    {
        _customerService.Setup(x => x.GetCustomerById("buyer")).ReturnsAsync((Customer)null);

        await _handler.Handle(new OrderPlacedEvent(_order), CancellationToken.None);

        _approvalService.Verify(x => x.Insert(It.IsAny<OrderApproval>()), Times.Never);
        _logger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_FailingSend_DoesNotStopTheOthers()
    {
        Staff(Approver("a1"), Approver("a2"));
        _messages.Setup(x => x.SendOrderApprovalRequestedMessage(It.IsAny<OrderApproval>(), It.IsAny<Customer>(),
                It.Is<Customer>(c => c.Id == "a1"), It.IsAny<Store>(), It.IsAny<string>(), null))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        await _handler.Handle(new OrderPlacedEvent(_order), CancellationToken.None);

        _messages.Verify(x => x.SendOrderApprovalRequestedMessage(It.IsAny<OrderApproval>(), _buyer,
            It.IsAny<Customer>(), It.IsAny<Store>(), "en", null), Times.Exactly(3));
    }
}
