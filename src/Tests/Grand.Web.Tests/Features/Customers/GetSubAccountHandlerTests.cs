using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Web.Features.Handlers.Customers;
using Grand.Web.Features.Models.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Globalization;

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
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        try
        {
            await ReturnsRoleAndLimit();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    private async Task ReturnsRoleAndLimit()
    {
        _customerServiceMock.Setup(c => c.GetCustomerById("sub")).ReturnsAsync(new Customer {
            Id = "sub", OwnerId = "owner", Email = "sub@x.com",
            SubAccountRole = SubAccountRole.Approver, OrderApprovalLimit = 750.25
        });

        var model = await _handler.Handle(Query("sub"), default);

        Assert.AreEqual(SubAccountRole.Approver, model.Role);
        Assert.AreEqual("750.25", model.OrderApprovalLimit);
    }

    [TestMethod]
    [DataRow("pl-PL", "1500,5")]
    [DataRow("de-DE", "1500,5")]
    [DataRow("en-US", "1500.5")]
    public async Task Get_FormatsTheLimitInTheCurrentCulture(string culture, string expected)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            _customerServiceMock.Setup(c => c.GetCustomerById("sub")).ReturnsAsync(
                new Customer { Id = "sub", OwnerId = "owner", Email = "sub@x.com", OrderApprovalLimit = 1500.5 });

            var model = await _handler.Handle(Query("sub"), default);

            Assert.AreEqual(expected, model.OrderApprovalLimit);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [TestMethod]
    public async Task Get_NoLimit_IsEmptyText()
    {
        _customerServiceMock.Setup(c => c.GetCustomerById("sub"))
            .ReturnsAsync(new Customer { Id = "sub", OwnerId = "owner", Email = "sub@x.com" });

        var model = await _handler.Handle(Query("sub"), default);

        Assert.AreEqual(string.Empty, model.OrderApprovalLimit);
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
