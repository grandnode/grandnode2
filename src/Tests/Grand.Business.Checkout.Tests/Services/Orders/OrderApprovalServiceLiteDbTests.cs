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

    [TestInitialize]
    public void Init()
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
        Assert.AreEqual(OrderApprovalStatus.Approved, stored.Status);
        Assert.AreEqual("staff", stored.DecidedByCustomerId);
    }

    [TestMethod]
    public async Task Close_AlreadyClosed_Loses()
    {
        var approval = await AddPending();
        var first = await _repository.GetByIdAsync(approval.Id);
        var second = await _repository.GetByIdAsync(approval.Id);

        Assert.IsTrue(await _service.Close(first, OrderApprovalStatus.Approved, "staff"));
        Assert.IsFalse(await _service.Close(second, OrderApprovalStatus.Withdrawn));

        Assert.AreEqual(OrderApprovalStatus.Approved, (await _repository.GetByIdAsync(approval.Id)).Status);
    }

    [TestMethod]
    public async Task Reopen_MakesItDecidableAgain()
    {
        var approval = await AddPending();
        await _service.Close(approval, OrderApprovalStatus.Rejected, "staff");

        await _service.Reopen(approval);

        Assert.AreEqual(OrderApprovalStatus.Pending, (await _repository.GetByIdAsync(approval.Id)).Status);
        Assert.IsTrue(await _service.Close(approval, OrderApprovalStatus.Approved, "staff"));
    }
}
