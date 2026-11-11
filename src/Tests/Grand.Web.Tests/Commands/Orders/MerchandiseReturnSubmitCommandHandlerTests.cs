using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.Commands.Handler.Orders;
using Grand.Web.Commands.Models.Orders;
using Grand.Web.Models.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Commands.Orders;

[TestClass]
public class MerchandiseReturnSubmitCommandHandlerTests
{
    private MerchandiseReturnSubmitCommandHandler _handler;
    private Mock<IMediator> _mediatorMock;
    private Mock<IMerchandiseReturnService> _merchandiseReturnServiceMock;
    private Mock<IResourceLock> _resourceLockMock;
    private Order _order;

    [TestInitialize]
    public void Init()
    {
        _order = new Order { Id = "o1" };
        _order.OrderItems.Add(new OrderItem { Id = "a", ProductId = "p", Quantity = 5 });

        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(new Customer { Id = "c1" });
        workContext.Setup(x => x.WorkingLanguage).Returns(new Language { Id = "l1" });
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(x => x.CurrentStore).Returns(new Domain.Stores.Store { Id = "s1" });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);
        contextAccessor.Setup(x => x.StoreContext).Returns(storeContext.Object);

        _mediatorMock = new Mock<IMediator>();
        _merchandiseReturnServiceMock = new Mock<IMerchandiseReturnService>();
        _resourceLockMock = new Mock<IResourceLock>();
        _resourceLockMock.Setup(x => x.Acquire(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());

        _handler = new MerchandiseReturnSubmitCommandHandler(contextAccessor.Object, _mediatorMock.Object,
            _merchandiseReturnServiceMock.Object, new Mock<IMerchandiseReturnMessageProviderService>().Object,
            new Mock<IGroupService>().Object, new LanguageSettings(), _resourceLockMock.Object);
    }

    private void SetupReturnable(int quantity)
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetReturnableQuantitiesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int> { { "a", quantity } });
    }

    private MerchandiseReturnSubmitCommand Command(int quantity)
    {
        var model = new MerchandiseReturnModel { OrderId = _order.Id };
        model.Items.Add(new MerchandiseReturnModel.OrderItemModel { Id = "a", Quantity = quantity });
        return new MerchandiseReturnSubmitCommand { Model = model, Order = _order, Address = new Address() };
    }

    [TestMethod]
    public async Task Handle_QuantityAboveReturnable_InsertsNothing()
    {
        //Arrange - another return took 3 of the 5 in the meantime
        SetupReturnable(2);

        //Act
        var result = await _handler.Handle(Command(5), CancellationToken.None);

        //Assert - not a smaller return the customer did not ask for
        Assert.IsNull(result);
        _merchandiseReturnServiceMock.Verify(x => x.InsertMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_NothingLeftToReturn_InsertsNothing()
    {
        //Arrange
        SetupReturnable(0);

        //Act
        var result = await _handler.Handle(Command(1), CancellationToken.None);

        //Assert
        Assert.IsNull(result);
        _merchandiseReturnServiceMock.Verify(x => x.InsertMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_QuantityWithinReturnable_TakesTheQuantity()
    {
        //Arrange
        SetupReturnable(5);

        //Act
        var result = await _handler.Handle(Command(3), CancellationToken.None);

        //Assert
        Assert.AreEqual(3, result.MerchandiseReturnItems.Single().Quantity);
    }

    [TestMethod]
    public async Task Handle_ChecksAndInsertsWhileHoldingTheOrderLock()
    {
        //Arrange - record when the lock is taken and let go, against the check and the insert
        var steps = new List<string>();
        var held = new Mock<IAsyncDisposable>();
        held.Setup(x => x.DisposeAsync()).Callback(() => steps.Add("release")).Returns(ValueTask.CompletedTask);
        _resourceLockMock.Setup(x => x.Acquire(It.Is<IEnumerable<string>>(k => k.Single() == "MerchandiseReturn:o1"),
                It.IsAny<CancellationToken>()))
            .Callback(() => steps.Add("acquire")).ReturnsAsync(held.Object);
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetReturnableQuantitiesQuery>(), It.IsAny<CancellationToken>()))
            .Callback(() => steps.Add("check")).ReturnsAsync(new Dictionary<string, int> { { "a", 5 } });
        _merchandiseReturnServiceMock.Setup(x => x.InsertMerchandiseReturn(It.IsAny<MerchandiseReturn>()))
            .Callback(() => steps.Add("insert")).Returns(Task.CompletedTask);

        //Act
        await _handler.Handle(Command(3), CancellationToken.None);

        //Assert
        CollectionAssert.AreEqual(new[] { "acquire", "check", "insert", "release" }, steps);
    }
}
