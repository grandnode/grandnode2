using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Messages.Commands.Handlers.Messages;
using Grand.Business.Messages.Commands.Tokens;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Commands;

[TestClass]
public class GetOrderApprovalTokensCommandHandlerTests
{
    private GetOrderApprovalTokensCommandHandler _handler;
    private Dictionary<string, string> _resources;
    private Mock<ITranslationService> _translationService;
    private Mock<IOrderService> _orderService;

    [TestInitialize]
    public void Init()
    {
        //resources the language has; a missing one gives the default, or an empty string when asked for that
        _resources = new Dictionary<string, string>();
        _translationService = new Mock<ITranslationService>();
        _translationService
            .Setup(x => x.GetResource(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns((string key, string _, string defaultValue, bool returnEmpty) =>
                _resources.TryGetValue(key, out var value) ? value : returnEmpty ? string.Empty : defaultValue);
        _orderService = new Mock<IOrderService>();
        _orderService.Setup(x => x.GetOrderById("order1")).ReturnsAsync(new Order { Id = "order1", OrderNumber = 1042 });
        _handler = new GetOrderApprovalTokensCommandHandler(_translationService.Object, _orderService.Object);
    }

    private static OrderApproval Approval()
    {
        return new OrderApproval {
            Id = "approval1",
            OrderId = "order1",
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
            Language = new Language { Id = "lang1" }
        };
    }

    [TestMethod]
    public async Task Handle_WithoutAnOrderId_FallsBackToTheApprovalUrlAndLeavesTheNumberEmpty()
    {
        var approval = Approval();
        approval.OrderId = string.Empty;

        var tokens = await _handler.Handle(Command(approval), default);

        Assert.AreEqual(string.Empty, tokens.OrderNumber);
        Assert.AreEqual("https://store.example/account/orderapproval?id=approval1", tokens.OrderUrl);
    }

    [TestMethod]
    public async Task Handle_EncodesTheStatus()
    {
        _resources[$"Enums.{typeof(OrderApprovalStatus).FullName}.Pending"] = "<b>Pending</b>";

        var tokens = await _handler.Handle(Command(Approval()), default);

        Assert.IsFalse(tokens.Status.Contains('<'));
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
        Assert.AreEqual("https://store.example/orderdetails/order1", result.OrderUrl);
    }

    [TestMethod]
    public async Task Handle_HostOverridesStoreUrl()
    {
        //Act
        var result = await _handler.Handle(
            Command(Approval(), new DomainHost { Url = "https://shop.example/" }), CancellationToken.None);

        //Assert
        Assert.AreEqual("https://shop.example/account/orderapproval?id=approval1", result.Url);
        Assert.AreEqual("https://shop.example/orderdetails/order1", result.OrderUrl);
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

    [TestMethod]
    public async Task Handle_TranslatesTheStatusInTheEmailLanguage()
    {
        //Arrange
        _resources["Enums.Grand.Domain.Orders.OrderApprovalStatus.Rejected"] = "Odrzucone";
        var approval = Approval();
        approval.Status = OrderApprovalStatus.Rejected;

        //Act
        var result = await _handler.Handle(Command(approval), CancellationToken.None);

        //Assert
        Assert.AreEqual("Odrzucone", result.Status);
        _translationService.Verify(x => x.GetResource("Enums.Grand.Domain.Orders.OrderApprovalStatus.Rejected",
            "lang1", It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_TranslatesTableHeadersAndSkuLabel()
    {
        //Arrange
        _resources["OrderApproval.Email.Product"] = "Produkt";
        _resources["OrderApproval.Email.Quantity"] = "Ilosc";
        _resources["OrderApproval.Email.Price"] = "Cena";
        _resources["OrderApproval.Email.Total"] = "Razem";
        _resources["OrderApproval.Email.Sku"] = "Kod";
        var approval = Approval();
        approval.Items.Add(new OrderApprovalItem { ProductName = "Desk", Sku = "D-1", Quantity = 1 });

        //Act
        var result = await _handler.Handle(Command(approval), CancellationToken.None);

        //Assert
        StringAssert.Contains(result.ItemsHtml, ">Produkt</th>");
        StringAssert.Contains(result.ItemsHtml, ">Ilosc</th>");
        StringAssert.Contains(result.ItemsHtml, ">Cena</th>");
        StringAssert.Contains(result.ItemsHtml, ">Razem</th>");
        StringAssert.Contains(result.ItemsHtml, "Kod: D-1");
        Assert.IsFalse(result.ItemsHtml.Contains("Quantity"));
        Assert.IsFalse(result.ItemsHtml.Contains("SKU"));
    }

    [TestMethod]
    public async Task Handle_MissingResources_FallBackToEnglish()
    {
        //Arrange
        var approval = Approval();
        approval.Items.Add(new OrderApprovalItem { ProductName = "Desk", Sku = "D-1", Quantity = 1 });

        //Act
        var result = await _handler.Handle(Command(approval), CancellationToken.None);

        //Assert
        Assert.AreEqual("Pending", result.Status);
        StringAssert.Contains(result.ItemsHtml, ">Product</th>");
        StringAssert.Contains(result.ItemsHtml, ">Quantity</th>");
        StringAssert.Contains(result.ItemsHtml, ">Price</th>");
        StringAssert.Contains(result.ItemsHtml, ">Total</th>");
        StringAssert.Contains(result.ItemsHtml, "SKU: D-1");
    }

    [TestMethod]
    public async Task Handle_EncodesTranslatedLabels()
    {
        //Arrange - a translator's text must not break the table
        _resources["OrderApproval.Email.Product"] = "<i>Produkt</i> & co";

        //Act
        var result = await _handler.Handle(Command(Approval()), CancellationToken.None);

        //Assert
        StringAssert.Contains(result.ItemsHtml, "&lt;i&gt;Produkt&lt;/i&gt; &amp; co");
        Assert.IsFalse(result.ItemsHtml.Contains("<i>"));
    }

    [TestMethod]
    public async Task Handle_FillsTheNumberOfTheOrderAwaitingTheDecision()
    {
        //Act
        var result = await _handler.Handle(Command(Approval()), CancellationToken.None);

        //Assert
        Assert.AreEqual("1042", result.OrderNumber);
    }

    [TestMethod]
    public async Task Handle_OrderGone_LeavesTheNumberEmptyAndLinksToTheRequest()
    {
        //Arrange
        var approval = Approval();
        approval.OrderId = "deleted";

        //Act
        var result = await _handler.Handle(Command(approval), CancellationToken.None);

        //Assert
        Assert.AreEqual(string.Empty, result.OrderNumber);
        Assert.AreEqual("https://store.example/account/orderapproval?id=approval1", result.OrderUrl);
    }
}
