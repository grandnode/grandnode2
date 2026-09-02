using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain;
using Grand.Domain.Discounts;
using Grand.Domain.Permissions;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Mediator;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Discounts;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

/// <summary>
/// ARCH-001 / Task 7b: regression coverage for the Admin-only "Applied to vendors" region on
/// Grand.Web.Admin.Controllers.DiscountController. This region has no Store counterpart (Store's
/// original DiscountController never had a Vendor tab) so it lives directly on the concrete Admin
/// controller rather than BaseDiscountController. These tests lock in the CURRENT, unchanged
/// behavior of VendorList/VendorDelete/VendorAddPopup(get)/VendorAddPopupList/VendorAddPopup(post)
/// so Task 9 can safely fold them into a thin BaseDiscountController subclass later.
/// </summary>
[TestClass]
public class DiscountControllerVendorTests
{
    private Mock<IDiscountViewModelService> _vmService = null!;
    private Mock<IDiscountService> _service = null!;
    private Mock<IVendorService> _vendorService = null!;
    private DiscountController _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<Grand.Web.AdminShared.Mapper.VendorProfile>();
        });
        AutoMapperConfig.Init(mapperConfig);

        _vmService = new Mock<IDiscountViewModelService>();
        _service = new Mock<IDiscountService>();
        _vendorService = new Mock<IVendorService>();

        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _sut = new DiscountController(
            _vmService.Object,
            _service.Object,
            translationServiceMock.Object,
            Mock.Of<IContextAccessor>(),
            Mock.Of<IDateTimeService>(),
            Mock.Of<IGroupService>(),
            Mock.Of<IDiscountProviderLoader>(),
            Mock.Of<IMediator>());
    }

    [TestMethod]
    public async Task VendorAddPopupList_NeverPassesAStoreIdArgument()
    {
        // IVendorService.GetAllVendors(name, pageIndex, pageSize, showHidden) has no storeId
        // parameter at all — vendors aren't store-scoped. This test guards against a future edit
        // accidentally introducing store-scoping to this call.
        var model = new DiscountModel.AddVendorToDiscountModel { DiscountId = "1" };
        _vendorService.Setup(x => x.GetAllVendors(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<Vendor>(new List<Vendor>(), 0, 10, 0));

        await _sut.VendorAddPopupList(new DataSourceRequest { Page = 1, PageSize = 10 }, model, _vendorService.Object);

        _vendorService.Verify(x => x.GetAllVendors(model.SearchVendorName, 0, 10, true), Times.Once);
        // Confirm the overload actually invoked is the 4-arg (no storeId) signature by checking
        // the interface only exposes this one GetAllVendors method.
        var method = typeof(IVendorService).GetMethod(nameof(IVendorService.GetAllVendors));
        Assert.IsNotNull(method);
        Assert.IsFalse(method!.GetParameters().Any(p => p.Name != null && p.Name.Contains("store", StringComparison.OrdinalIgnoreCase)),
            "IVendorService.GetAllVendors should not have gained a storeId parameter");
    }

    [TestMethod]
    public async Task VendorDelete_MissingVendor_ThrowsException()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _vendorService.Setup(x => x.GetVendorById("missing-vendor")).ReturnsAsync((Vendor)null);

        await Assert.ThrowsAsync<Exception>(() =>
            _sut.VendorDelete("1", "missing-vendor", _vendorService.Object));
    }

    [TestMethod]
    public async Task VendorList_MissingDiscount_ThrowsException()
    {
        _service.Setup(x => x.GetDiscountById("missing")).ReturnsAsync((Discount)null);

        await Assert.ThrowsAsync<Exception>(() =>
            _sut.VendorList(new DataSourceRequest(), "missing", _vendorService.Object));
    }

    [TestMethod]
    public async Task VendorList_ReturnsVendorsMappedFromDiscount()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        var vendors = new List<Vendor> { new() { Id = "v1", Name = "Vendor 1" } };
        _vendorService.Setup(x => x.GetAllVendorsByDiscount("1")).ReturnsAsync(vendors);

        var result = await _sut.VendorList(new DataSourceRequest(), "1", _vendorService.Object) as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual(1, data.Total);
    }

    [TestMethod]
    public async Task VendorDelete_ExistingVendor_DeletesAndReturnsEmptyJson()
    {
        var discount = new Discount { Id = "1" };
        var vendor = new Vendor { Id = "v1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _vendorService.Setup(x => x.GetVendorById("v1")).ReturnsAsync(vendor);

        await _sut.VendorDelete("1", "v1", _vendorService.Object);

        _vmService.Verify(x => x.DeleteVendor(discount, vendor), Times.Once);
    }

    [TestMethod]
    public void VendorAddPopup_Get_ReturnsViewWithEmptyModel()
    {
        var result = _sut.VendorAddPopup("1") as ViewResult;

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result!.Model, typeof(DiscountModel.AddVendorToDiscountModel));
    }

    [TestMethod]
    public async Task VendorAddPopup_Post_MissingDiscount_ThrowsException()
    {
        var model = new DiscountModel.AddVendorToDiscountModel { DiscountId = "missing" };
        _service.Setup(x => x.GetDiscountById("missing")).ReturnsAsync((Discount)null);

        await Assert.ThrowsAsync<Exception>(() => _sut.VendorAddPopup(model));
    }

    [TestMethod]
    public async Task VendorAddPopup_Post_WithSelectedVendors_InsertsVendors()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        var model = new DiscountModel.AddVendorToDiscountModel { DiscountId = "1", SelectedVendorIds = ["v1"] };

        await _sut.VendorAddPopup(model);

        _vmService.Verify(x => x.InsertVendorToDiscountModel(model), Times.Once);
    }
}

/// <summary>
/// Regression test for ARCH-001 authorization attributes on Discount's Admin-only applied-to-vendors
/// region methods. Ensures VendorList, VendorDelete, both VendorAddPopup overloads, and
/// VendorAddPopupList carry the required [PermissionAuthorizeAction] attributes. These methods live
/// only on the Admin concrete DiscountController — Grand.Web.Store.Controllers.DiscountController
/// must never gain equivalents.
/// </summary>
[TestClass]
public class DiscountControllerVendorAttributeTests
{
    [TestMethod]
    public void VendorList_HasPermissionAuthorizeActionPreview()
    {
        var method = typeof(DiscountController).GetMethod("VendorList");
        Assert.IsNotNull(method, "VendorList method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "VendorList missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Preview, attr!.PermissionAction, "VendorList should require Preview permission");
    }

    [TestMethod]
    public void VendorDelete_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(DiscountController).GetMethod("VendorDelete");
        Assert.IsNotNull(method, "VendorDelete method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "VendorDelete missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "VendorDelete should require Edit permission");
    }

    [TestMethod]
    public void VendorAddPopup_Get_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(DiscountController).GetMethod("VendorAddPopup", [typeof(string)]);
        Assert.IsNotNull(method, "VendorAddPopup(string) method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "VendorAddPopup(string) missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "VendorAddPopup(string) should require Edit permission");
    }

    [TestMethod]
    public void VendorAddPopupList_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(DiscountController).GetMethod("VendorAddPopupList");
        Assert.IsNotNull(method, "VendorAddPopupList method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "VendorAddPopupList missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "VendorAddPopupList should require Edit permission");
    }

    [TestMethod]
    public void VendorAddPopup_Post_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(DiscountController).GetMethod("VendorAddPopup", [typeof(DiscountModel.AddVendorToDiscountModel)]);
        Assert.IsNotNull(method, "VendorAddPopup(AddVendorToDiscountModel) method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "VendorAddPopup(AddVendorToDiscountModel) missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "VendorAddPopup(AddVendorToDiscountModel) should require Edit permission");
    }

    [TestMethod]
    public void StoreDiscountController_HasNoVendorActions()
    {
        // Guards Task 12's expectation: Grand.Web.Store's DiscountController must never gain
        // Vendor actions, since Store never had an "Applied to vendors" tab.
        var storeControllerType = Type.GetType(
            "Grand.Web.Store.Controllers.DiscountController, Grand.Web.Store");
        Assert.IsNotNull(storeControllerType, "Grand.Web.Store.Controllers.DiscountController type not found");
        Assert.IsNull(storeControllerType!.GetMethod("VendorList"), "Store DiscountController should not have VendorList");
        Assert.IsNull(storeControllerType.GetMethod("VendorDelete"), "Store DiscountController should not have VendorDelete");
        Assert.IsNull(storeControllerType.GetMethod("VendorAddPopup"), "Store DiscountController should not have VendorAddPopup");
        Assert.IsNull(storeControllerType.GetMethod("VendorAddPopupList"), "Store DiscountController should not have VendorAddPopupList");
    }
}
