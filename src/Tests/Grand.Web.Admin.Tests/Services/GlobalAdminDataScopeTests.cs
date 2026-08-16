using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class GlobalAdminDataScopeTests
{
    [TestMethod]
    public async Task HasAccess_AlwaysReturnsTrue()
    {
        var scope = new GlobalAdminDataScope<Product>();
        var result = await scope.HasAccess(new Product());
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ApplyScope_ReturnsQueryUnchanged()
    {
        var scope = new GlobalAdminDataScope<Product>();
        var query = new[] { new Product { Id = "1" }, new Product { Id = "2" } }.AsQueryable();

        var result = scope.ApplyScope(query);

        CollectionAssert.AreEqual(query.ToList(), result.ToList());
    }

    [TestMethod]
    public void DefaultStoreId_IsNull()
    {
        var scope = new GlobalAdminDataScope<Product>();
        Assert.IsNull(scope.DefaultStoreId);
    }

    [TestMethod]
    public void ResourceKeyPrefix_IsAdmin()
    {
        var scope = new GlobalAdminDataScope<Product>();
        Assert.AreEqual("Admin", scope.ResourceKeyPrefix);
    }
}
