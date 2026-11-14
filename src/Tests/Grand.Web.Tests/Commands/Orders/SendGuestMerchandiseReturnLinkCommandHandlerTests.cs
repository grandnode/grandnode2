using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Infrastructure.Caching;
using Grand.Web.Commands.Handler.Orders;
using Grand.Web.Commands.Models.Orders;
using Grand.Web.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Commands.Orders;

/// <summary>
///     A guest asks for a return link with the order number and e-mail address: the link goes to the order's address
///     only when they match an order of the store placed without an account, and nothing tells which case it was
/// </summary>
[TestClass]
public class SendGuestMerchandiseReturnLinkCommandHandlerTests
{
    private readonly Customer _guest = new() { Id = "g1" };
    private Mock<IGroupService> _groups;
    private SendGuestMerchandiseReturnLinkCommandHandler _handler;
    private Mock<IMerchandiseReturnMessageProviderService> _messages;
    private Order _order;
    private List<Order> _otherOrders;
    private IDataProtectionProvider _provider;
    private string _sentToken;

    [TestInitialize]
    public void Init()
    {
        _order = new Order {
            Id = "o1", Code = "K7Q2M9XA", StoreId = "s1", CustomerId = "g1", CustomerEmail = "guest@example.com"
        };
        _otherOrders = [];
        var orders = new Mock<IOrderService>();
        orders.Setup(x => x.GetOrdersByCode(It.IsAny<string>())).ReturnsAsync((string code) =>
            code == "K7Q2M9XA" ? _otherOrders.Append(_order).ToList() : []);
        var customers = new Mock<ICustomerService>();
        customers.Setup(x => x.GetCustomerById("g1")).ReturnsAsync(_guest);
        _groups = new Mock<IGroupService>();
        _groups.Setup(x => x.IsRegistered(_guest)).ReturnsAsync(false);
        _messages = new Mock<IMerchandiseReturnMessageProviderService>();
        _messages.Setup(x => x.SendMerchandiseReturnGuestLinkCustomerMessage(It.IsAny<Order>(), It.IsAny<string>(),
                It.IsAny<DomainHost>()))
            .Callback<Order, string, DomainHost>((_, token, _) => _sentToken = token)
            .ReturnsAsync(1);

        //the cache runs what it is asked to acquire once per key, as within its expiry
        var seen = new HashSet<string>();
        var cache = new Mock<ICacheBase>();
        cache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<bool>>>(), It.IsAny<int>()))
            .Returns<string, Func<Task<bool>>, int>(async (key, acquire, _) => !seen.Add(key) || await acquire());
        cache.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .Callback<string, bool>((key, _) => seen.Remove(key))
            .Returns(Task.CompletedTask);

        _provider = new EphemeralDataProtectionProvider();
        _handler = new SendGuestMerchandiseReturnLinkCommandHandler(orders.Object, customers.Object, _groups.Object,
            _messages.Object, _provider, cache.Object,
            NullLogger<SendGuestMerchandiseReturnLinkCommandHandler>.Instance);
    }

    private Task<bool> Send(string email = "guest@example.com", string orderCode = "K7Q2M9XA", string storeId = "s1")
    {
        return _handler.Handle(new SendGuestMerchandiseReturnLinkCommand {
            OrderCode = orderCode, Email = email, Store = new Domain.Stores.Store { Id = storeId }
        }, CancellationToken.None);
    }

    private void VerifySent(Times times)
    {
        _messages.Verify(x => x.SendMerchandiseReturnGuestLinkCustomerMessage(It.IsAny<Order>(), It.IsAny<string>(),
            It.IsAny<DomainHost>()), times);
    }

    [TestMethod]
    public async Task Match_SendsLink()
    {
        Assert.IsTrue(await Send(" Guest@Example.com "));

        VerifySent(Times.Once());
        Assert.AreEqual("o1", MerchandiseReturnGuestGrant.LinkOrderId(_provider, _sentToken, "s1"));
    }

    [TestMethod]
    public async Task GuestOrderWithoutCustomerEmail_MatchesBillingEmail()
    {
        //a guest's order keeps the address typed at checkout in its billing address only
        _order.CustomerEmail = null;
        _order.BillingAddress = new Address { Email = "guest@example.com" };

        Assert.IsTrue(await Send("Guest@example.com"));
        VerifySent(Times.Once());
    }

    [TestMethod]
    public async Task NoOrder_NothingSent()
    {
        Assert.IsFalse(await Send(orderCode: "K7Q2M9XB"));
        VerifySent(Times.Never());
    }

    [TestMethod]
    public async Task CodeWithSpaces_SendsLink()
    {
        Assert.IsTrue(await Send(orderCode: " K7Q2M9XA "));
        VerifySent(Times.Once());
    }

    [TestMethod]
    public async Task CodeShared_OrderOfStoreWithEmailChosen()
    {
        //a code is random, not unique: another store's order and one placed with another address share it
        _otherOrders.Add(new Order
            { Id = "o2", Code = "K7Q2M9XA", StoreId = "s2", CustomerId = "g1", CustomerEmail = "guest@example.com" });
        _otherOrders.Add(new Order
            { Id = "o3", Code = "K7Q2M9XA", StoreId = "s1", CustomerId = "g1", CustomerEmail = "other@example.com" });

        Assert.IsTrue(await Send());
        Assert.AreEqual("o1", MerchandiseReturnGuestGrant.LinkOrderId(_provider, _sentToken, "s1"));
    }

    [TestMethod]
    public async Task OrderWithoutEmail_NothingSent()
    {
        _order.CustomerEmail = null;

        Assert.IsFalse(await Send(""));
        VerifySent(Times.Never());
    }

    [TestMethod]
    public async Task WrongEmail_NothingSent()
    {
        Assert.IsFalse(await Send("someone@example.com"));
        VerifySent(Times.Never());
    }

    [TestMethod]
    public async Task RegisteredCustomer_NothingSent()
    {
        _groups.Setup(x => x.IsRegistered(_guest)).ReturnsAsync(true);

        Assert.IsFalse(await Send());
        VerifySent(Times.Never());
    }

    [TestMethod]
    public async Task OtherStore_NothingSent()
    {
        Assert.IsFalse(await Send(storeId: "s2"));
        VerifySent(Times.Never());
    }

    [TestMethod]
    public async Task DeletedOrder_NothingSent()
    {
        _order.Deleted = true;

        Assert.IsFalse(await Send());
        VerifySent(Times.Never());
    }

    [TestMethod]
    public async Task SecondRequestWithinWindow_NothingSent()
    {
        Assert.IsTrue(await Send());
        Assert.IsFalse(await Send());

        VerifySent(Times.Once());
    }

    [TestMethod]
    public async Task SendFails_FalseWithoutException()
    {
        //a failure only a matching order can meet must not answer differently from a mismatch
        _messages.Setup(x => x.SendMerchandiseReturnGuestLinkCustomerMessage(It.IsAny<Order>(), It.IsAny<string>(),
            It.IsAny<DomainHost>())).ThrowsAsync(new InvalidOperationException("no e-mail account"));

        Assert.IsFalse(await Send());
    }

    [TestMethod]
    public async Task InactiveTemplate_False()
    {
        _messages.Setup(x => x.SendMerchandiseReturnGuestLinkCustomerMessage(It.IsAny<Order>(), It.IsAny<string>(),
            It.IsAny<DomainHost>())).ReturnsAsync(0);

        Assert.IsFalse(await Send());
    }

    [TestMethod]
    public async Task LinkNotSent_GuestMayAskAgain()
    {
        //Arrange - the first link fails, the second goes out
        _messages.SetupSequence(x => x.SendMerchandiseReturnGuestLinkCustomerMessage(It.IsAny<Order>(),
                It.IsAny<string>(), It.IsAny<DomainHost>()))
            .ThrowsAsync(new InvalidOperationException("no e-mail account"))
            .ReturnsAsync(0)
            .ReturnsAsync(1);

        //Act
        var thrown = await Send();
        var inactive = await Send();
        var sent = await Send();

        //Assert
        Assert.IsFalse(thrown);
        Assert.IsFalse(inactive);
        Assert.IsTrue(sent);
        VerifySent(Times.Exactly(3));
    }
}
