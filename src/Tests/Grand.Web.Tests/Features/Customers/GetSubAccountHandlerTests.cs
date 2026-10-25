using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Web.Features.Handlers.Customers;
using Grand.Web.Features.Models.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Customers;

[TestClass]
public class GetSubAccountHandlerTests
{
    private Mock<ICustomerService> _customerServiceMock;
    private GetSubAccountHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _handler = new GetSubAccountHandler(_customerServiceMock.Object);
    }

    private static GetSubAccount Query(string id) =>
        new() { CurrentCustomer = new Customer { Id = "owner" }, CustomerId = id };

    [TestMethod]
    public async Task Handle_OwnSubAccount_ReturnsIt()
    {
        _customerServiceMock.Setup(c => c.GetCustomerById("sub"))
            .ReturnsAsync(new Customer { Id = "sub", OwnerId = "owner", Email = "sub@x.com", Active = true });

        var model = await _handler.Handle(Query("sub"), default);

        Assert.IsNotNull(model);
        Assert.AreEqual("sub", model.Id);
        Assert.AreEqual("sub@x.com", model.Email);
        Assert.IsTrue(model.Active);
    }

    [TestMethod]
    public async Task Get_ReturnsRoleAndLimit()
    {
        _customerServiceMock.Setup(c => c.GetCustomerById("sub")).ReturnsAsync(new Customer {
            Id = "sub", OwnerId = "owner", Email = "sub@x.com",
            SubAccountRole = SubAccountRole.Approver, OrderApprovalLimit = 750.25
        });

        var model = await _handler.Handle(Query("sub"), default);

        Assert.AreEqual(SubAccountRole.Approver, model.Role);
        Assert.AreEqual(750.25, model.OrderApprovalLimit);
    }

    [TestMethod]
    [DataRow("another-owner", false)]
    [DataRow("", false)]
    [DataRow("owner", true)]
    public async Task Handle_NotAnOwnLiveSubAccount_ReturnsNull(string ownerId, bool deleted)
    {
        //an empty model used to be returned, which rendered as a blank edit form
        _customerServiceMock.Setup(c => c.GetCustomerById("x"))
            .ReturnsAsync(new Customer { Id = "x", OwnerId = ownerId, Deleted = deleted, Email = "x@x.com" });

        Assert.IsNull(await _handler.Handle(Query("x"), default));
    }

    [TestMethod]
    public async Task Handle_MissingAccount_ReturnsNull()
    {
        Assert.IsNull(await _handler.Handle(Query("missing"), default));
    }
}
