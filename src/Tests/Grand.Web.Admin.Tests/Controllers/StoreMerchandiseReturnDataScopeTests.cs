using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class StoreMerchandiseReturnDataScopeTests
{
    private static StoreMerchandiseReturnDataScope Build(string staffStoreId)
    {
        var customer = new Customer { StaffStoreId = staffStoreId };
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(customer);
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);
        return new StoreMerchandiseReturnDataScope(contextAccessorMock.Object);
    }

    [TestMethod]
    public async Task HasAccess_MatchingStoreId_True()
    {
        var scope = Build("store-1");
        Assert.IsTrue(await scope.HasAccess(new MerchandiseReturn { StoreId = "store-1" }));
    }

    [TestMethod]
    public async Task HasAccess_MismatchedStoreId_False()
    {
        var scope = Build("store-1");
        Assert.IsFalse(await scope.HasAccess(new MerchandiseReturn { StoreId = "store-2" }));
    }

    [TestMethod]
    public async Task HasAccess_NullEntity_False()
    {
        var scope = Build("store-1");
        Assert.IsFalse(await scope.HasAccess(null));
    }

    [TestMethod]
    public void ScopeDefaults_StoreScoped()
    {
        var scope = Build("store-1");
        Assert.AreEqual("store-1", scope.DefaultStoreId);
        Assert.IsNull(scope.DefaultVendorId);
        Assert.AreEqual("Admin", scope.ResourceKeyPrefix);
        Assert.IsTrue(scope.ShowStoreSelector);
    }

    [TestMethod]
    public async Task CanView_NotOverridden_MatchesHasAccess()
    {
        // No loose/strict split for this entity on Store (spec §2.3) - CanView must fall through to
        // the interface default (HasAccess), not be separately implemented.
        var scope = Build("store-1");
        var entity = new MerchandiseReturn { StoreId = "store-2" };
        Assert.AreEqual(await scope.HasAccess(entity), await ((IAdminDataScope<MerchandiseReturn>)scope).CanView(entity));
    }
}
