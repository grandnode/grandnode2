using Grand.Business.Core.Extensions;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Extensions;

[TestClass]
public class MerchandiseReturnStatusTransitionsTests
{
    [TestMethod]
    [DataRow(MerchandiseReturnStatus.Pending, new[] { 10, 15, 20, 30 })]
    [DataRow(MerchandiseReturnStatus.Accepted, new[] { 15, 40, 20, 30 })]
    [DataRow(MerchandiseReturnStatus.Received, new[] { 40 })]
    [DataRow(MerchandiseReturnStatus.Rejected, new[] { 0 })]
    [DataRow(MerchandiseReturnStatus.Cancelled, new[] { 0 })]
    [DataRow(MerchandiseReturnStatus.Completed, new int[0])]
    public void NextStatuses_FollowTheTable(MerchandiseReturnStatus from, int[] expected)
    {
        CollectionAssert.AreEqual(expected, from.NextStatuses().Select(x => (int)x).ToArray());
    }

    [TestMethod]
    public void CanChangeTo_SameStatus_False()
    {
        Assert.IsFalse(MerchandiseReturnStatus.Pending.CanChangeTo(MerchandiseReturnStatus.Pending));
    }

    [TestMethod]
    public void CanChangeTo_ReceivedToRejected_False()
    {
        Assert.IsFalse(MerchandiseReturnStatus.Received.CanChangeTo(MerchandiseReturnStatus.Rejected));
    }

    [TestMethod]
    public void CanChangeTo_AcceptedToReceived_True()
    {
        Assert.IsTrue(MerchandiseReturnStatus.Accepted.CanChangeTo(MerchandiseReturnStatus.Received));
    }

    [TestMethod]
    public void Received_TakesQuantity()
    {
        Assert.IsTrue(MerchandiseReturnStatus.Received.TakesQuantity());
    }
}
