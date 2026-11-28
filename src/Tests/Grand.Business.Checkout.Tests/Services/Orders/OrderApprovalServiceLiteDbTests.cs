using Grand.Business.Checkout.Services.Orders;
using Grand.Data.Tests.LiteDb;
using Grand.Domain.Orders;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

/// <summary>
///     Closing a request filters on its status: this holds on LiteDB too, or nobody there could decide anything
/// </summary>
[TestClass]
public class OrderApprovalServiceLiteDbTests
{
    private LiteDBRepositoryMock<OrderApproval> _repository;
    private OrderApprovalService _service;

    public OrderApprovalServiceLiteDbTests()
    {
        _repository = new LiteDBRepositoryMock<OrderApproval>();
        _service = new OrderApprovalService(_repository, new OrderApprovalSettings(), new Mock<IMediator>().Object);
    }

    private async Task<OrderApproval> AddPending()
    {
        var approval = new OrderApproval { Status = OrderApprovalStatus.Pending, OrderId = "order1" };
        await _repository.InsertAsync(approval);
        return approval;
    }

    [TestMethod]
    public async Task Close_Pending_Closes()
    {
        var approval = await AddPending();

        Assert.IsTrue(await _service.Close(approval, OrderApprovalStatus.Approved, "staff"));

        var stored = await _repository.GetByIdAsync(approval.Id);
        Assert.IsNotNull(stored);
        Assert.AreEqual(OrderApprovalStatus.Approved, stored.Status);
        Assert.AreEqual("staff", stored.DecidedByCustomerId);
    }

    [TestMethod]
    public async Task Close_AlreadyClosed_Loses()
    {
        var approval = await AddPending();
        var first = await _repository.GetByIdAsync(approval.Id);
        var second = await _repository.GetByIdAsync(approval.Id);

        Assert.IsNotNull(first);
        Assert.IsTrue(await _service.Close(first, OrderApprovalStatus.Approved, "staff"));
        Assert.IsNotNull(second);
        Assert.IsFalse(await _service.Close(second, OrderApprovalStatus.Withdrawn));

        var orderApproval = await _repository.GetByIdAsync(approval.Id);
        Assert.IsNotNull(orderApproval);
        Assert.AreEqual(OrderApprovalStatus.Approved, orderApproval.Status);
    }

    [TestMethod]
    public async Task Reopen_MakesItDecidableAgain()
    {
        var approval = await AddPending();
        await _service.Close(approval, OrderApprovalStatus.Rejected, "staff");

        await _service.Reopen(approval);

        var orderApproval = await _repository.GetByIdAsync(approval.Id);
        Assert.IsNotNull(orderApproval);
        Assert.AreEqual(OrderApprovalStatus.Pending, orderApproval.Status);
        Assert.IsTrue(await _service.Close(approval, OrderApprovalStatus.Approved, "staff"));
    }
}
