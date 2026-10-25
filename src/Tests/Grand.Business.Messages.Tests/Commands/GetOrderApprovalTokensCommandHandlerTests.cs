using Grand.Business.Messages.Commands.Handlers.Messages;
using Grand.Business.Messages.Commands.Tokens;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Messages.Tests.Commands;

[TestClass]
public class GetOrderApprovalTokensCommandHandlerTests
{
    private GetOrderApprovalTokensCommandHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _handler = new GetOrderApprovalTokensCommandHandler();
    }

    private static OrderApproval Approval()
    {
        return new OrderApproval {
            Id = "approval1",
            Status = OrderApprovalStatus.Pending,
            Summary = new OrderApprovalSummary { Total = "EUR 120.00" }
        };
    }

    private static GetOrderApprovalTokensCommand Command(OrderApproval approval, DomainHost host = null)
    {
        return new GetOrderApprovalTokensCommand {
            Approval = approval,
            Buyer = new Customer { Email = "buyer@example.com", Username = "buyer" },
            Store = new Store { Url = "https://store.example/" },
            Host = host,
            Language = new Language()
        };
    }

    [TestMethod]
    public async Task Handle_EncodesCommentAndItemValues()
    {
        //Arrange
        var approval = Approval();
        approval.DecisionComment = "<script>alert('x')</script> & more";
        approval.Items.Add(new OrderApprovalItem {
            ProductName = "<b>Desk</b>", Sku = "SKU<1>", Quantity = 2, AttributeDescription = "Color: <i>red</i>",
            UnitPrice = "EUR 10.00", LineTotal = "EUR 20.00"
        });

        //Act
        var result = await _handler.Handle(Command(approval), CancellationToken.None);

        //Assert
        Assert.AreEqual("&lt;script&gt;alert(&#39;x&#39;)&lt;/script&gt; &amp; more", result.DecisionComment);
        Assert.IsFalse(result.ItemsHtml.Contains("<b>Desk</b>"));
        Assert.IsFalse(result.ItemsHtml.Contains("<i>red</i>"));
        Assert.IsFalse(result.ItemsHtml.Contains("SKU<1>"));
        StringAssert.Contains(result.ItemsHtml, "&lt;b&gt;Desk&lt;/b&gt;");
        StringAssert.Contains(result.ItemsHtml, "Color: &lt;i&gt;red&lt;/i&gt;");
        StringAssert.Contains(result.ItemsHtml, "SKU&lt;1&gt;");
        StringAssert.Contains(result.ItemsHtml, "EUR 20.00");
    }

    [TestMethod]
    public async Task Handle_BuildsAbsoluteLinks()
    {
        //Act
        var result = await _handler.Handle(Command(Approval()), CancellationToken.None);

        //Assert
        Assert.AreEqual("https://store.example/account/orderapproval?id=approval1", result.Url);
        Assert.AreEqual("https://store.example/cart", result.CartUrl);
    }

    [TestMethod]
    public async Task Handle_HostOverridesStoreUrl()
    {
        //Act
        var result = await _handler.Handle(
            Command(Approval(), new DomainHost { Url = "https://shop.example/" }), CancellationToken.None);

        //Assert
        Assert.AreEqual("https://shop.example/account/orderapproval?id=approval1", result.Url);
        Assert.AreEqual("https://shop.example/cart", result.CartUrl);
    }

    [TestMethod]
    public async Task Handle_FillsBuyerStatusAndTotal()
    {
        //Arrange
        var approval = Approval();
        approval.Status = OrderApprovalStatus.Rejected;

        //Act
        var result = await _handler.Handle(Command(approval), CancellationToken.None);

        //Assert
        Assert.AreEqual("buyer@example.com", result.BuyerEmail);
        Assert.AreEqual("Rejected", result.Status);
        Assert.AreEqual("EUR 120.00", result.Total);
    }

    [TestMethod]
    public async Task Handle_NoItemsNoComment_ReturnsEmptyValues()
    {
        //Act
        var result = await _handler.Handle(Command(Approval()), CancellationToken.None);

        //Assert
        Assert.AreEqual(string.Empty, result.DecisionComment);
        Assert.IsNotNull(result.ItemsHtml);
    }
}
