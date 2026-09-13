using Grand.Domain.Customers;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class StoreTaxCategoryDataScopeTests
{
    private static StoreTaxCategoryDataScope CreateScope(string staffStoreId)
    {
        var customer = new Customer { StaffStoreId = staffStoreId };
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer).Returns(customer);
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(c => c.WorkContext).Returns(workContext.Object);
        return new StoreTaxCategoryDataScope(contextAccessor.Object);
    }

    [TestMethod]
    public async Task HasAccess_ExactStoreMatch_ReturnsTrue()
    {
        var scope = CreateScope("store-1");
        var category = new TaxCategory { StoreId = "store-1" };
        Assert.IsTrue(await scope.HasAccess(category));
    }

    [TestMethod]
    public async Task HasAccess_DifferentStore_ReturnsFalse()
    {
        var scope = CreateScope("store-1");
        var category = new TaxCategory { StoreId = "store-2" };
        Assert.IsFalse(await scope.HasAccess(category));
    }

    [TestMethod]
    public async Task HasAccess_GlobalCategory_ReturnsFalse()
    {
        // Empty StoreId (global) is NOT owned by any store — a store manager can never
        // edit/delete a global tax category, matching the original controller's
        // `taxCategory.StoreId != CurrentStoreId` check (empty never equals a real store id).
        var scope = CreateScope("store-1");
        var category = new TaxCategory { StoreId = "" };
        Assert.IsFalse(await scope.HasAccess(category));
    }

    [TestMethod]
    public async Task HasAccess_NullEntity_ReturnsFalse()
    {
        var scope = CreateScope("store-1");
        Assert.IsFalse(await scope.HasAccess(null!));
    }

    [TestMethod]
    public void DefaultStoreId_ReturnsStaffStoreId()
    {
        var scope = CreateScope("store-1");
        Assert.AreEqual("store-1", scope.DefaultStoreId);
    }

    [TestMethod]
    public void ShowStoreSelector_ReturnsFalse()
    {
        var scope = CreateScope("store-1");
        Assert.IsFalse(scope.ShowStoreSelector);
    }
}
