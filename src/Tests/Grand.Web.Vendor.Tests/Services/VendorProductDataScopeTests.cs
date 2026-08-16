using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Vendor.Tests.Services;

[TestClass]
public class VendorProductDataScopeTests
{
    private Mock<IContextAccessor> _contextAccessor = null!;
    private const string VendorId = "vendor-1";

    [TestInitialize]
    public void Setup()
    {
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentVendor).Returns(new Domain.Vendors.Vendor { Id = VendorId });
        _contextAccessor = new Mock<IContextAccessor>();
        _contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);
    }

    [TestMethod]
    public async Task HasAccess_OwnProduct_ReturnsTrue()
    {
        var scope = new VendorProductDataScope(_contextAccessor.Object);
        Assert.IsTrue(await scope.HasAccess(new Product { VendorId = VendorId }));
    }

    [TestMethod]
    public async Task HasAccess_OtherVendorsProduct_ReturnsFalse()
    {
        var scope = new VendorProductDataScope(_contextAccessor.Object);
        Assert.IsFalse(await scope.HasAccess(new Product { VendorId = "vendor-2" }));
    }

    [TestMethod]
    public async Task HasAccess_NullProduct_ReturnsFalse()
    {
        var scope = new VendorProductDataScope(_contextAccessor.Object);
        Assert.IsFalse(await scope.HasAccess(null!));
    }

    [TestMethod]
    public void ApplyScope_FiltersToOwnVendorId()
    {
        var scope = new VendorProductDataScope(_contextAccessor.Object);
        var query = new[]
        {
            new Product { Id = "1", VendorId = VendorId },
            new Product { Id = "2", VendorId = "vendor-2" }
        }.AsQueryable();

        var result = scope.ApplyScope(query).ToList();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("1", result[0].Id);
    }

    [TestMethod]
    public void DefaultStoreId_IsNull()
    {
        var scope = new VendorProductDataScope(_contextAccessor.Object);
        Assert.IsNull(scope.DefaultStoreId);
    }

    [TestMethod]
    public void ResourceKeyPrefix_IsVendor()
    {
        var scope = new VendorProductDataScope(_contextAccessor.Object);
        Assert.AreEqual("Vendor", scope.ResourceKeyPrefix);
    }
}
