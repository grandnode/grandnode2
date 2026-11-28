using DiscountRules.Standard.Controllers;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Plugins.DiscountRules;

/// <summary>
///     A rule window opened or saved for a discount that does not exist (deleted meanwhile, or a crafted id) answers
///     with a message, as a missing rule does, instead of a server error
/// </summary>
[TestClass]
public class DiscountRuleControllerUnknownDiscountTests
{
    private const string Missing = "missing";
    private readonly IContextAccessor _contextAccessor;
    private readonly IDiscountService _discountService = new Mock<IDiscountService>().Object;
    private readonly IRequestPermissionService _permissionService;

    public DiscountRuleControllerUnknownDiscountTests()
    {
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext.CurrentCustomer).Returns(new Customer());
        _contextAccessor = contextAccessor.Object;
        var permissionService = new Mock<IRequestPermissionService>();
        permissionService.Setup(x => x.Authorize(StandardPermission.ManageDiscounts)).ReturnsAsync(true);
        _permissionService = permissionService.Object;
    }

    private DiscountRulesHasAllProductsController HasAllProducts() =>
        new(_discountService, _permissionService, _contextAccessor, new Mock<IRequestTranslationService>().Object,
            new Mock<IStoreService>().Object, new Mock<IVendorService>().Object, new Mock<IProductService>().Object,
            new Mock<IEnumTranslationService>().Object);

    private DiscountRulesHasOneProductController HasOneProduct() =>
        new(_discountService, _permissionService, _contextAccessor, new Mock<IRequestTranslationService>().Object,
            new Mock<IStoreService>().Object, new Mock<IVendorService>().Object, new Mock<IProductService>().Object,
            new Mock<IEnumTranslationService>().Object);

    private static void AssertMessage(IActionResult result)
    {
        Assert.AreEqual("Failed to load discount.", (result as ContentResult)?.Content);
    }

    [TestMethod]
    public async Task CustomerGroups_UnknownDiscount_Message()
    {
        var controller = new DiscountRulesCustomerGroupsController(_discountService, _contextAccessor,
            new Mock<IGroupService>().Object, _permissionService);
        AssertMessage(await controller.Configure(Missing, ""));
        AssertMessage(await controller.Configure(Missing, "", "group"));
    }

    [TestMethod]
    public async Task HadSpentAmount_UnknownDiscount_Message()
    {
        var controller = new DiscountRulesHadSpentAmountController(_discountService, _contextAccessor, _permissionService);
        AssertMessage(await controller.Configure(Missing, ""));
        AssertMessage(await controller.Configure(Missing, "", 10));
    }

    [TestMethod]
    public async Task ShoppingCartAmount_UnknownDiscount_Message()
    {
        var controller = new DiscountRulesShoppingCartAmountController(_discountService, _contextAccessor, _permissionService);
        AssertMessage(await controller.Configure(Missing, ""));
        AssertMessage(await controller.Configure(Missing, "", 10));
    }

    [TestMethod]
    public async Task HasAllProducts_UnknownDiscount_Message()
    {
        AssertMessage(await HasAllProducts().Configure(Missing, ""));
        AssertMessage(await HasAllProducts().Configure(Missing, "", "p1"));
    }

    [TestMethod]
    public async Task HasOneProduct_UnknownDiscount_Message()
    {
        AssertMessage(await HasOneProduct().Configure(Missing, ""));
        AssertMessage(await HasOneProduct().Configure(Missing, "", "p1"));
    }
}
