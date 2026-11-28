using DiscountRules.Standard.Controllers;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Domain.Customers;
using Grand.Domain.Discounts;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Plugins.DiscountRules;

[TestClass]
public class DiscountRuleControllerScopeTests
{
    private readonly Customer _customer = new();
    private DiscountRulesShoppingCartAmountController _controller;
    private Mock<IDiscountService> _discountService;

    public DiscountRuleControllerScopeTests()
    {
        _discountService = new Mock<IDiscountService>();
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext.CurrentCustomer).Returns(_customer);
        var permissionService = new Mock<IRequestPermissionService>();
        permissionService.Setup(x => x.Authorize(StandardPermission.ManageDiscounts)).ReturnsAsync(true);
        _controller = new DiscountRulesShoppingCartAmountController(_discountService.Object, contextAccessor.Object,
            permissionService.Object);
    }

    private Task<IActionResult> Save(Discount discount)
    {
        _discountService.Setup(x => x.GetDiscountById(discount.Id)).ReturnsAsync(discount);
        return _controller.Configure(discount.Id, "", 10);
    }

    private Task<IActionResult> Open(Discount discount)
    {
        _discountService.Setup(x => x.GetDiscountById(discount.Id)).ReturnsAsync(discount);
        return _controller.Configure(discount.Id, "");
    }

    [TestMethod]
    public async Task StoreStaff_DiscountOfAnotherStore_AccessDenied()
    {
        _customer.StaffStoreId = "store1";
        var result = await Save(new Discount { Id = "d1", LimitedToStores = true, Stores = ["store2"] });

        Assert.AreEqual("Access denied", (result as ContentResult)?.Content);
        _discountService.Verify(x => x.UpdateDiscount(It.IsAny<Discount>()), Times.Never);
    }

    [TestMethod]
    public async Task StoreStaff_GlobalDiscount_AccessDenied()
    {
        _customer.StaffStoreId = "store1";
        var result = await Save(new Discount { Id = "d1" });

        Assert.AreEqual("Access denied", (result as ContentResult)?.Content);
        _discountService.Verify(x => x.UpdateDiscount(It.IsAny<Discount>()), Times.Never);
    }

    [TestMethod]
    public async Task StoreStaff_DiscountOfTheirStoreAlone_Saved()
    {
        _customer.StaffStoreId = "store1";
        var result = await Save(new Discount { Id = "d1", LimitedToStores = true, Stores = ["store1"] });

        Assert.IsInstanceOfType<JsonResult>(result);
        _discountService.Verify(x => x.UpdateDiscount(It.IsAny<Discount>()), Times.Once);
    }

    [TestMethod]
    public async Task Admin_GlobalDiscount_Saved()
    {
        var result = await Save(new Discount { Id = "d1" });

        Assert.IsInstanceOfType<JsonResult>(result);
        _discountService.Verify(x => x.UpdateDiscount(It.IsAny<Discount>()), Times.Once);
    }

    [TestMethod]
    public async Task StoreStaff_OpenGlobalDiscount_RuleShown()
    {
        _customer.StaffStoreId = "store1";
        var result = await Open(new Discount { Id = "d1" });

        Assert.IsInstanceOfType<ViewResult>(result);
    }

    [TestMethod]
    public async Task StoreStaff_OpenDiscountSharedWithTheirStore_RuleShown()
    {
        _customer.StaffStoreId = "store1";
        var result = await Open(new Discount { Id = "d1", LimitedToStores = true, Stores = ["store1", "store2"] });

        Assert.IsInstanceOfType<ViewResult>(result);
    }

    [TestMethod]
    public async Task StoreStaff_OpenDiscountOfAnotherStore_AccessDenied()
    {
        _customer.StaffStoreId = "store1";
        var result = await Open(new Discount { Id = "d1", LimitedToStores = true, Stores = ["store2"] });

        Assert.AreEqual("Access denied", (result as ContentResult)?.Content);
    }
}
