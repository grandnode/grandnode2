using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class StoreReportDataScopeTests
{
    private static StoreReportDataScope Build(string staffStoreId)
    {
        var customer = new Customer { StaffStoreId = staffStoreId };
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(customer);
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);
        return new StoreReportDataScope(contextAccessorMock.Object);
    }

    [TestMethod]
    public void StoreId_ReturnsCurrentStaffStoreId()
    {
        var scope = Build("store-1");
        Assert.AreEqual("store-1", scope.StoreId);
    }

    [TestMethod]
    public void VendorId_AlwaysEmpty()
    {
        var scope = Build("store-1");
        Assert.AreEqual("", scope.VendorId);
    }

    [TestMethod]
    public void Selectors_BothHidden()
    {
        var scope = Build("store-1");
        Assert.IsFalse(scope.ShowStoreSelector);
        Assert.IsFalse(scope.ShowVendorSelector);
    }

    [TestMethod]
    public void ResourceKeyPrefix_IsAdmin()
    {
        // Store reuses Admin's resource keys today — same precedent as every prior phase.
        var scope = Build("store-1");
        Assert.AreEqual("Admin", scope.ResourceKeyPrefix);
    }

    [TestMethod]
    public void CanIncludeProduct_NotOverridden_AlwaysTrue()
    {
        var scope = Build("store-1");
        Assert.IsTrue(scope.CanIncludeProduct(new Product { Id = "p1" }));
    }
}
