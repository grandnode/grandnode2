using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure;
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
    private Mock<IMerchandiseReturnMessageProviderService> _messageProviderMock;
    private Order _order;
    private MerchandiseReturnSubmitRequest _request;
    private MerchandiseReturnSubmitResult _result;
    private Mock<IMerchandiseReturnWorkflowService> _workflowMock;
    private OrderSettings _orderSettings;

    [TestInitialize]
    public void Init()
    {
        _order = new Order { Id = "o1", StoreId = "order-store", CustomerLanguageId = "customer-language" };
        _order.OrderItems.Add(new OrderItem { Id = "a", ProductId = "p", Quantity = 5 });

        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(new Customer { Id = "c1", OwnerId = "owner" });
        workContext.Setup(x => x.WorkingLanguage).Returns(new Language { Id = "l1" });
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(x => x.CurrentStore).Returns(new Domain.Stores.Store { Id = "s1" });
        storeContext.Setup(x => x.CurrentHost).Returns(new Domain.Stores.DomainHost { HostName = "host" });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);
        contextAccessor.Setup(x => x.StoreContext).Returns(storeContext.Object);

        _result = new MerchandiseReturnSubmitResult();
        _workflowMock = new Mock<IMerchandiseReturnWorkflowService>();
        _workflowMock.Setup(x => x.Submit(It.IsAny<MerchandiseReturnSubmitRequest>()))
            .Callback<MerchandiseReturnSubmitRequest>(r => _request = r)
            .ReturnsAsync(() => _result);
        _messageProviderMock = new Mock<IMerchandiseReturnMessageProviderService>();

        _orderSettings = new OrderSettings();
        _handler = new MerchandiseReturnSubmitCommandHandler(contextAccessor.Object, _workflowMock.Object,
            _messageProviderMock.Object, new Mock<IGroupService>().Object,
            new LanguageSettings { DefaultAdminLanguageId = "admin-language" }, _orderSettings);
    }

    private MerchandiseReturnSubmitCommand Command(int quantity)
    {
        var model = new MerchandiseReturnModel { OrderId = _order.Id, Comments = "comments" };
        model.Items.Add(new MerchandiseReturnModel.OrderItemModel {
            Id = "a", Quantity = quantity, MerchandiseReturnReasonId = "rr1", MerchandiseReturnActionId = "ra1"
        });
        return new MerchandiseReturnSubmitCommand { Model = model, Order = _order, Address = new Address() };
    }

    [TestMethod]
    public async Task Handle_PassesTheOrderCustomerStoreAndLanguage()
    {
        //Act
        await _handler.Handle(Command(3), CancellationToken.None);

        //Assert
        Assert.AreSame(_order, _request.Order);
        //the order's store, whose staff handle the return, not the store the request came through
        Assert.AreEqual("order-store", _request.StoreId);
        Assert.AreEqual("c1", _request.CustomerId);
        Assert.AreEqual("owner", _request.OwnerId);
        Assert.AreEqual("l1", _request.LanguageId);
        Assert.AreEqual("comments", _request.Comments);
        Assert.AreEqual(new MerchandiseReturnSubmitItem("a", 3, "rr1", "ra1"), _request.Items.Single());
    }

    [TestMethod]
    public async Task Handle_RequestedActionHidden_SavesNoAction()
    {
        //a crafted post names one anyway
        _orderSettings.MerchandiseReturns_HideRequestedAction = true;

        await _handler.Handle(Command(3), CancellationToken.None);

        Assert.AreEqual(new MerchandiseReturnSubmitItem("a", 3, "rr1", null), _request.Items.Single());
    }

    [TestMethod]
    public async Task SubmitCommand_PassesChoiceWithStrippedPrefix()
    {
        //Arrange - the in-store option with the pickup point picked in its form, among the other posted fields
        var command = Command(3);
        command.Model.ReturnShippingProvider = "ReturnShipping.InStore";
        command.Model.ReturnShippingOption = "instore";
        command.Form = new Dictionary<string, string> {
            ["returnshipping.pickupPointId"] = "point1",
            ["Comments"] = "comments",
            ["ReturnShippingProvider"] = "ReturnShipping.InStore"
        };

        //Act
        await _handler.Handle(command, CancellationToken.None);

        //Assert
        Assert.AreEqual("ReturnShipping.InStore", _request.ReturnShipping.ProviderSystemName);
        Assert.AreEqual("instore", _request.ReturnShipping.OptionCode);
        Assert.AreEqual(1, _request.ReturnShipping.Form.Count);
        Assert.AreEqual("point1", _request.ReturnShipping.Form["pickupPointId"]);
    }

    [TestMethod]
    public async Task SubmitCommand_NoMethodChosen_PassesNoChoice()
    {
        //Arrange - no return method in the store: the form posts none
        var command = Command(3);
        command.Form = new Dictionary<string, string> { ["Comments"] = "comments" };

        //Act
        await _handler.Handle(command, CancellationToken.None);

        //Assert
        Assert.IsNull(_request.ReturnShipping);
    }

    [TestMethod]
    public async Task Handle_TwoReturns_SendsBothMessagesForEach()
    {
        //Arrange - the store's items and a vendor's items became two returns
        var storeReturn = new MerchandiseReturn { Id = "r1" };
        var vendorReturn = new MerchandiseReturn { Id = "r2", VendorId = "v1" };
        _result.MerchandiseReturns.Add(storeReturn);
        _result.MerchandiseReturns.Add(vendorReturn);

        //Act
        var result = await _handler.Handle(Command(3), CancellationToken.None);

        //Assert
        Assert.AreSame(_result, result);
        foreach (var merchandiseReturn in new[] { storeReturn, vendorReturn })
        {
            _messageProviderMock.Verify(x => x.SendNewMerchandiseReturnStoreOwnerMessage(merchandiseReturn, _order,
                "admin-language", It.IsAny<Domain.Stores.DomainHost>()), Times.Once);
            _messageProviderMock.Verify(x => x.SendNewMerchandiseReturnCustomerMessage(merchandiseReturn, _order,
                "customer-language", It.IsAny<Domain.Stores.DomainHost>()), Times.Once);
        }
    }

    [TestMethod]
    public async Task Handle_Refused_SendsNothing()
    {
        //Arrange
        _result.Error = MerchandiseReturnErrors.SubmitQuantityExceeded;

        //Act
        var result = await _handler.Handle(Command(9), CancellationToken.None);

        //Assert
        Assert.AreEqual(MerchandiseReturnErrors.SubmitQuantityExceeded, result.Error);
        _messageProviderMock.VerifyNoOtherCalls();
    }
}
