using Grand.Business.Core.Extensions;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Services.Shipping;

[TestClass]
public class MerchandiseReturnShippingExtensionsTests
{
    [TestMethod]
    public void CustomerCost_FeesFree_IsZero()
    {
        Assert.AreEqual(0, MerchandiseReturnShippingExtensions.CustomerCost(12.5, MerchandiseReturnFees.Free, false, false));
    }

    [TestMethod]
    public void CustomerCost_AnyFreeReason_IsZero()
    {
        Assert.AreEqual(0, MerchandiseReturnShippingExtensions.CustomerCost(12.5, MerchandiseReturnFees.CustomerPays, true, false));
    }

    [TestMethod]
    public void CustomerCost_AllWithoutRefund_IsZero()
    {
        Assert.AreEqual(0, MerchandiseReturnShippingExtensions.CustomerCost(12.5, MerchandiseReturnFees.CustomerPays, false, true));
    }

    [TestMethod]
    public void CustomerCost_NotSpecified_KeepsOptionCost()
    {
        Assert.AreEqual(12.5, MerchandiseReturnShippingExtensions.CustomerCost(12.5, MerchandiseReturnFees.NotSpecified, false, false));
    }

    [TestMethod]
    public void LabelLockKey_DiffersFromQuantityLockKey()
    {
        Assert.AreNotEqual(MerchandiseReturnExtensions.QuantityLockKey("x"), MerchandiseReturnExtensions.LabelLockKey("x"));
    }
}
