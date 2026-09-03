using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class StoreGiftVoucherDataScopeTests
{
    private static StoreGiftVoucherDataScope CreateScope(string staffStoreId)
    {
        var customer = new Customer { StaffStoreId = staffStoreId };
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer).Returns(customer);
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(c => c.WorkContext).Returns(workContext.Object);
        return new StoreGiftVoucherDataScope(contextAccessor.Object);
    }

    [TestMethod]
    public async Task HasAccess_OwnStore_ReturnsTrue()
    {
        var scope = CreateScope("store-1");
        var result = await scope.HasAccess(new GiftVoucher { StoreId = "store-1" });
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task HasAccess_OtherStore_ReturnsFalse()
    {
        var scope = CreateScope("store-1");
        var result = await scope.HasAccess(new GiftVoucher { StoreId = "store-2" });
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task HasAccess_GlobalVoucher_ReturnsFalse()
    {
        var scope = CreateScope("store-1");
        var result = await scope.HasAccess(new GiftVoucher { StoreId = "" });
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task HasAccess_NullEntity_ReturnsFalse()
    {
        var scope = CreateScope("store-1");
        var result = await scope.HasAccess(null);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task CanView_OwnStore_ReturnsTrue()
    {
        var scope = CreateScope("store-1");
        var result = await scope.CanView(new GiftVoucher { StoreId = "store-1" });
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task CanView_GlobalVoucher_ReturnsTrue()
    {
        var scope = CreateScope("store-1");
        var result = await scope.CanView(new GiftVoucher { StoreId = "" });
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task CanView_NullStoreId_ReturnsTrue()
    {
        var scope = CreateScope("store-1");
        var result = await scope.CanView(new GiftVoucher { StoreId = null });
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task CanView_OtherStore_ReturnsFalse()
    {
        var scope = CreateScope("store-1");
        var result = await scope.CanView(new GiftVoucher { StoreId = "store-2" });
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task CanView_NullEntity_ReturnsFalse()
    {
        var scope = CreateScope("store-1");
        var result = await scope.CanView(null);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void DefaultStoreId_ReturnsStaffStoreId()
    {
        var scope = CreateScope("store-1");
        Assert.AreEqual("store-1", scope.DefaultStoreId);
    }
}
