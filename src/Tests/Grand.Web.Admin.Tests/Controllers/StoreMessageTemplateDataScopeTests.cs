using Grand.Domain.Customers;
using Grand.Domain.Messages;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class StoreMessageTemplateDataScopeTests
{
    private static StoreMessageTemplateDataScope CreateScope(string staffStoreId)
    {
        var customer = new Customer { StaffStoreId = staffStoreId };
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer).Returns(customer);
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(c => c.WorkContext).Returns(workContext.Object);
        return new StoreMessageTemplateDataScope(contextAccessor.Object);
    }

    [TestMethod]
    public async Task HasAccess_ExclusivelyOwnedByCurrentStore_ReturnsTrue()
    {
        var scope = CreateScope("store-1");
        var template = new MessageTemplate { LimitedToStores = true, Stores = ["store-1"] };
        Assert.IsTrue(await scope.HasAccess(template));
    }

    [TestMethod]
    public async Task HasAccess_SharedWithMultipleStores_ReturnsFalse()
    {
        var scope = CreateScope("store-1");
        var template = new MessageTemplate { LimitedToStores = true, Stores = ["store-1", "store-2"] };
        Assert.IsFalse(await scope.HasAccess(template));
    }

    [TestMethod]
    public async Task HasAccess_OwnedByOtherStore_ReturnsFalse()
    {
        var scope = CreateScope("store-1");
        var template = new MessageTemplate { LimitedToStores = true, Stores = ["store-2"] };
        Assert.IsFalse(await scope.HasAccess(template));
    }

    [TestMethod]
    public async Task HasAccess_Global_ReturnsFalse()
    {
        var scope = CreateScope("store-1");
        var template = new MessageTemplate { LimitedToStores = false, Stores = [] };
        Assert.IsFalse(await scope.HasAccess(template));
    }

    [TestMethod]
    public async Task HasAccess_NullEntity_ReturnsFalse()
    {
        var scope = CreateScope("store-1");
        Assert.IsFalse(await scope.HasAccess(null));
    }

    [TestMethod]
    public async Task CanView_ExclusivelyOwned_ReturnsTrue()
    {
        var scope = CreateScope("store-1");
        var template = new MessageTemplate { LimitedToStores = true, Stores = ["store-1"] };
        Assert.IsTrue(await scope.CanView(template));
    }

    [TestMethod]
    public async Task CanView_SharedWithMultipleStores_IncludingCurrent_ReturnsTrue()
    {
        var scope = CreateScope("store-1");
        var template = new MessageTemplate { LimitedToStores = true, Stores = ["store-1", "store-2"] };
        Assert.IsTrue(await scope.CanView(template));
    }

    [TestMethod]
    public async Task CanView_Global_ReturnsTrue()
    {
        var scope = CreateScope("store-1");
        var template = new MessageTemplate { LimitedToStores = false, Stores = [] };
        Assert.IsTrue(await scope.CanView(template));
    }

    [TestMethod]
    public async Task CanView_OwnedByOtherStoreOnly_ReturnsFalse()
    {
        var scope = CreateScope("store-1");
        var template = new MessageTemplate { LimitedToStores = true, Stores = ["store-2"] };
        Assert.IsFalse(await scope.CanView(template));
    }

    [TestMethod]
    public async Task CanView_NullEntity_ReturnsFalse()
    {
        var scope = CreateScope("store-1");
        Assert.IsFalse(await scope.CanView(null));
    }

    [TestMethod]
    public void DefaultStoreId_ReturnsStaffStoreId()
    {
        var scope = CreateScope("store-1");
        Assert.AreEqual("store-1", scope.DefaultStoreId);
    }

    [TestMethod]
    public void ShowStoreSelector_IsFalse()
    {
        var scope = CreateScope("store-1");
        Assert.IsFalse(scope.ShowStoreSelector);
    }
}
