using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Infrastructure.Caching;
using Grand.Web.Commands.Handler.Orders;
using Grand.Web.Commands.Models.Orders;
using Grand.Web.Extensions;
using Microsoft.AspNetCore.DataProtection;
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
    private IDataProtectionProvider _provider;
    private string _sentToken;

    [TestInitialize]
    public void Init()
    {
        _order = new Order {
            Id = "o1", OrderNumber = 7, StoreId = "s1", CustomerId = "g1", CustomerEmail = "guest@example.com"
        };
        var orders = new Mock<IOrderService>();
        orders.Setup(x => x.GetOrderByNumber(7)).ReturnsAsync(() => _order);
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

        _provider = new EphemeralDataProtectionProvider();
        _handler = new SendGuestMerchandiseReturnLinkCommandHandler(orders.Object, customers.Object, _groups.Object,
            _messages.Object, _provider, cache.Object);
    }

    private Task<bool> Send(string email = "guest@example.com", int orderNumber = 7, string storeId = "s1")
    {
        return _handler.Handle(new SendGuestMerchandiseReturnLinkCommand {
            OrderNumber = orderNumber, Email = email, Store = new Domain.Stores.Store { Id = storeId }
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
    public async Task NoOrder_NothingSent()
    {
        Assert.IsFalse(await Send(orderNumber: 8));
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
    public async Task InactiveTemplate_False()
    {
        _messages.Setup(x => x.SendMerchandiseReturnGuestLinkCustomerMessage(It.IsAny<Order>(), It.IsAny<string>(),
            It.IsAny<DomainHost>())).ReturnsAsync(0);

        Assert.IsFalse(await Send());
    }
}
