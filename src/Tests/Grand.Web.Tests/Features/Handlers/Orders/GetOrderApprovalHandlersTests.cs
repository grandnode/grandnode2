using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Web.Common.Localization;
using Grand.Web.Features.Handlers.Orders;
using Grand.Web.Features.Models.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.Orders;

[TestClass]
public class GetOrderApprovalHandlersTests
{
    private Mock<IOrderApprovalService> _approvals;
    private Mock<ICustomerService> _customers;
    private GetOrderApprovalHandler _detailsHandler;
    private GetOrderApprovalsHandler _listHandler;
    private Grand.Domain.Stores.Store _store;

    [TestInitialize]
    public void Init()
    {
        _store = new Grand.Domain.Stores.Store { Id = "s1" };
        _approvals = new Mock<IOrderApprovalService>();
        _customers = new Mock<ICustomerService>();
        var dates = new Mock<IDateTimeService>();
        dates.Setup(d => d.ConvertToUserTime(It.IsAny<DateTime>(), DateTimeKind.Utc))
            .Returns((DateTime d, DateTimeKind _) => d);
        var enums = new Mock<IEnumTranslationService>();
        enums.Setup(e => e.GetTranslationEnum(It.IsAny<OrderApprovalStatus>()))
            .Returns((OrderApprovalStatus s) => s.ToString());
        _detailsHandler = new GetOrderApprovalHandler(_approvals.Object, _customers.Object, dates.Object,
            enums.Object);
        _listHandler = new GetOrderApprovalsHandler(_approvals.Object, _customers.Object, dates.Object,
            enums.Object);
    }

    private static OrderApproval Approval(string id = "a1", string buyer = "buyer",
        OrderApprovalStatus status = OrderApprovalStatus.Pending)
    {
        return new OrderApproval {
            Id = id, CustomerId = buyer, CompanyOwnerId = "owner", StoreId = "s1", Status = status,
            Items = [new OrderApprovalItem { ProductName = "Desk {{ 1 + 1 }}", Quantity = 2 }],
            Summary = new OrderApprovalSummary { Total = "300 EUR" }
        };
    }

    private Task<Grand.Web.Models.Orders.OrderApprovalModel> Details(Customer who, string id = "a1")
    {
        return _detailsHandler.Handle(new GetOrderApproval { Customer = who, Store = _store, Id = id },
            CancellationToken.None);
    }

    private void Found(OrderApproval approval)
    {
        _approvals.Setup(a => a.GetById(approval.Id, "owner")).ReturnsAsync(approval);
    }

    //details

    [TestMethod]
    public async Task Details_AskedWithTheViewersCompany()
    {
        var model = await Details(new Customer { Id = "someone", OwnerId = "other-owner" });

        Assert.IsNull(model);
        _approvals.Verify(a => a.GetById("a1", "other-owner"));
    }

    [TestMethod]
    public async Task Details_OtherStore_IsNull()
    {
        var approval = Approval();
        approval.StoreId = "s2";
        Found(approval);

        Assert.IsNull(await Details(new Customer { Id = "owner" }));
    }

    [TestMethod]
    public async Task Details_BuyerSeesOwnRequest_AndCanWithdrawIt()
    {
        Found(Approval());

        var model = await Details(new Customer { Id = "buyer", OwnerId = "owner" });

        Assert.IsNotNull(model);
        Assert.IsTrue(model.CanWithdraw);
        Assert.IsFalse(model.CanDecide);
        Assert.AreEqual(1, model.Items.Count);
        Assert.AreEqual("300 EUR", model.Summary.Total);
        Assert.AreEqual("Pending", model.Status);
    }

    [TestMethod]
    public async Task Details_ColleagueWithoutApproverRole_IsNull()
    {
        Found(Approval());

        var model = await Details(new Customer { Id = "colleague", OwnerId = "owner", SubAccountRole = SubAccountRole.Buyer });

        Assert.IsNull(model);
    }

    [TestMethod]
    public async Task Details_OwnerSeesCompanyRequest_AndCanDecideIt()
    {
        Found(Approval());
        _customers.Setup(c => c.GetCustomerById("buyer"))
            .ReturnsAsync(new Customer { Id = "buyer", Email = "b@x.test" });

        var model = await Details(new Customer { Id = "owner" });

        Assert.IsTrue(model.CanDecide);
        Assert.IsFalse(model.CanWithdraw);
        Assert.AreEqual("b@x.test", model.Buyer);
    }

    [TestMethod]
    public async Task Details_DecidedRequest_CannotBeDecidedAgain()
    {
        Found(Approval(status: OrderApprovalStatus.Rejected));

        var model = await Details(new Customer { Id = "owner" });

        Assert.IsFalse(model.CanDecide);
    }

    [TestMethod]
    public async Task Details_UsedRequest_CannotBeWithdrawn()
    {
        Found(Approval(status: OrderApprovalStatus.Used));

        var model = await Details(new Customer { Id = "buyer", OwnerId = "owner" });

        Assert.IsFalse(model.CanWithdraw);
    }

    //list

    private static IPagedList<OrderApproval> Page(params OrderApproval[] items)
    {
        return new PagedList<OrderApproval>(items, 0, 10, items.Length);
    }

    [TestMethod]
    public async Task List_OwnerGetsTheCompanyList()
    {
        _approvals.Setup(a => a.GetForCompany("owner", "s1", 0, 10)).ReturnsAsync(Page(Approval()));
        _customers.Setup(c => c.GetCustomersByIds(It.IsAny<string[]>()))
            .ReturnsAsync([new Customer { Id = "buyer", Email = "b@x.test" }]);

        var model = await _listHandler.Handle(new GetOrderApprovals {
            Customer = new Customer { Id = "owner" }, Store = _store
        }, CancellationToken.None);

        Assert.IsTrue(model.IsCompanyList);
        Assert.AreEqual(1, model.Approvals.Count);
        Assert.AreEqual("b@x.test", model.Approvals[0].Buyer);
        Assert.AreEqual("300 EUR", model.Approvals[0].Total);
        Assert.AreEqual(1, model.Approvals[0].ItemCount);
        Assert.IsTrue(model.Approvals[0].IsPending);
        _approvals.Verify(a => a.GetForCustomer(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task List_ApproverSubAccountGetsTheCompanyListOfItsOwner()
    {
        _approvals.Setup(a => a.GetForCompany("owner", "s1", 0, 10)).ReturnsAsync(Page());

        var model = await _listHandler.Handle(new GetOrderApprovals {
            Customer = new Customer { Id = "appr", OwnerId = "owner", SubAccountRole = SubAccountRole.Approver },
            Store = _store
        }, CancellationToken.None);

        Assert.IsTrue(model.IsCompanyList);
        _approvals.Verify(a => a.GetForCompany("owner", "s1", 0, 10));
    }

    [TestMethod]
    public async Task List_BuyerGetsOnlyOwnRequests()
    {
        _approvals.Setup(a => a.GetForCustomer("buyer", "s1", 1, 10)).ReturnsAsync(Page(Approval()));

        var model = await _listHandler.Handle(new GetOrderApprovals {
            Customer = new Customer { Id = "buyer", OwnerId = "owner" }, Store = _store,
            Command = new Grand.Web.Models.Orders.OrderPagingModel { PageNumber = 2 }
        }, CancellationToken.None);

        Assert.IsFalse(model.IsCompanyList);
        Assert.IsNull(model.Approvals[0].Buyer);
        _approvals.Verify(a => a.GetForCompany(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);
        _customers.Verify(c => c.GetCustomersByIds(It.IsAny<string[]>()), Times.Never);
    }

    [TestMethod]
    public async Task List_PageSizeIsCapped()
    {
        _approvals.Setup(a => a.GetForCompany("owner", "s1", 0, 50)).ReturnsAsync(Page());

        await _listHandler.Handle(new GetOrderApprovals {
            Customer = new Customer { Id = "owner" }, Store = _store,
            Command = new Grand.Web.Models.Orders.OrderPagingModel { PageSize = 100000 }
        }, CancellationToken.None);

        _approvals.Verify(a => a.GetForCompany("owner", "s1", 0, 50));
    }
}
