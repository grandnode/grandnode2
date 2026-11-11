using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Mediator;
using Grand.Web.Common.Localization;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Orders;
using Grand.Web.Validators.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Validators.Orders;

[TestClass]
public class MerchandiseReturnValidatorTests
{
    private Mock<IMediator> _mediatorMock;
    private Mock<IOrderService> _orderServiceMock;
    private Order _order;

    [TestInitialize]
    public void Init()
    {
        _order = new Order { Id = "o1" };
        _order.OrderItems.Add(new OrderItem { Id = "a", ProductId = "p", VendorId = "v1", Quantity = 2 });
        _order.OrderItems.Add(new OrderItem { Id = "b", ProductId = "p", VendorId = "v2", Quantity = 2 });

        _orderServiceMock = new Mock<IOrderService>();
        _orderServiceMock.Setup(x => x.GetOrderById("o1")).ReturnsAsync(_order);

        _mediatorMock = new Mock<IMediator>();
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetParseCustomAddressAttributes>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CustomAttribute>());
        SetupReturnable(new Dictionary<string, int> { { "a", 2 }, { "b", 2 } });
    }

    private void SetupReturnable(IReadOnlyDictionary<string, int> quantities)
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetReturnableQuantitiesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(quantities);
    }

    private MerchandiseReturnValidator Validator()
    {
        var addressAttributeParser = new Mock<IAddressAttributeParser>();
        addressAttributeParser.Setup(x => x.GetAttributeWarnings(It.IsAny<IList<CustomAttribute>>(), It.IsAny<string>()))
            .ReturnsAsync(new List<string>());
        var translationService = new Mock<IRequestTranslationService>();
        translationService.Setup(x => x.GetResource(It.IsAny<string>())).Returns((string key) => key);
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(x => x.CurrentStore).Returns(new Domain.Stores.Store { Id = "s1" });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.StoreContext).Returns(storeContext.Object);

        return new MerchandiseReturnValidator(new List<IValidatorConsumer<MerchandiseReturnModel>>(),
            new OrderSettings(), _orderServiceMock.Object, _mediatorMock.Object,
            addressAttributeParser.Object, translationService.Object, contextAccessor.Object);
    }

    private static MerchandiseReturnModel Model(string orderId, params (string Id, int Quantity)[] items)
    {
        var model = new MerchandiseReturnModel { OrderId = orderId };
        foreach (var (id, quantity) in items)
            model.Items.Add(new MerchandiseReturnModel.OrderItemModel { Id = id, Quantity = quantity });
        return model;
    }

    [TestMethod]
    public async Task Validate_QuantityWithinReturnable_IsValid()
    {
        var result = await Validator().ValidateAsync(Model("o1", ("a", 2)));

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task Validate_QuantityAboveReturnable_IsRejected()
    {
        //Arrange - one of the two was already returned
        SetupReturnable(new Dictionary<string, int> { { "a", 1 }, { "b", 2 } });

        //Act
        var result = await Validator().ValidateAsync(Model("o1", ("a", 2)));

        //Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "MerchandiseReturns.QuantityExceeded"));
    }

    [TestMethod]
    public async Task Validate_ItemNotInOrder_IsRejected()
    {
        var result = await Validator().ValidateAsync(Model("o1", ("x", 1)));

        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public async Task Validate_ProductDeleted_IsRejectedWithoutException()
    {
        //Arrange - the product of the item is gone, so nothing of it can be returned
        SetupReturnable(new Dictionary<string, int> { { "a", 0 }, { "b", 0 } });

        //Act
        var result = await Validator().ValidateAsync(Model("o1", ("a", 1)));

        //Assert
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public async Task Validate_OrderNotFound_IsRejectedWithoutException()
    {
        var result = await Validator().ValidateAsync(Model("missing", ("a", 1)));

        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public async Task Validate_NothingSubmitted_IsRejected()
    {
        var result = await Validator().ValidateAsync(Model("o1", ("a", 0)));

        Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "MerchandiseReturns.NoItemsSubmitted"));
    }

    [TestMethod]
    public async Task Validate_ItemsOfTwoVendors_IsRejected()
    {
        var result = await Validator().ValidateAsync(Model("o1", ("a", 1), ("b", 1)));

        Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "MerchandiseReturns.MultiVendorsItems"));
    }
}
