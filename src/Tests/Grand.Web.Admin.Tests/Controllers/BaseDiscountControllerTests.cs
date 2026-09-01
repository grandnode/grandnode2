using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Queries.Catalog;
using Grand.Domain;
using Grand.Domain.Discounts;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Mediator;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Discounts;
using Grand.Web.Common.DataSource;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseDiscountControllerTests
{
    // BaseDiscountController is abstract; minimal subclass so actions can be invoked directly.
    private class TestDiscountController(
        IDiscountViewModelService discountViewModelService,
        IDiscountService discountService,
        ITranslationService translationService,
        IDateTimeService dateTimeService,
        IMediator mediator,
        IAdminDataScope<Discount> scope)
        : BaseDiscountController(discountViewModelService, discountService, translationService, dateTimeService,
            mediator, scope);

    private Mock<IDiscountViewModelService> _vmService = null!;
    private Mock<IDiscountService> _service = null!;
    private Mock<IAdminDataScope<Discount>> _scope = null!;
    private Mock<IMediator> _mediator = null!;
    private TestDiscountController _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<DiscountProfile>());
        AutoMapperConfig.Init(mapperConfig);

        _vmService = new Mock<IDiscountViewModelService>();
        _service = new Mock<IDiscountService>();
        _scope = new Mock<IAdminDataScope<Discount>>();
        _mediator = new Mock<IMediator>();

        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _sut = new TestDiscountController(_vmService.Object, _service.Object,
            translationServiceMock.Object, Mock.Of<IDateTimeService>(), _mediator.Object, _scope.Object);

        var httpContext = new DefaultHttpContext();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
        urlHelperFactoryMock.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>())).Returns(new Mock<IUrlHelper>().Object);
        var requestServicesMock = new Mock<IServiceProvider>();
        requestServicesMock.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(loggerFactoryMock.Object);
        requestServicesMock.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactoryMock.Object);
        httpContext.RequestServices = requestServicesMock.Object;
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _sut.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    [TestMethod]
    public async Task Edit_Get_ScopeDeniesView_RedirectsToList()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.CanView(discount)).ReturnsAsync(false);

        var result = await _sut.Edit("1") as RedirectToActionResult;

        Assert.AreEqual("List", result!.ActionName);
    }

    [TestMethod]
    public async Task Edit_Get_ScopeAllowsView_ReturnsView()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.CanView(discount)).ReturnsAsync(true);

        var result = await _sut.Edit("1") as ViewResult;

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result!.Model, typeof(DiscountModel));
        _vmService.Verify(x => x.PrepareDiscountModel(It.IsAny<DiscountModel>(), discount), Times.Once);
    }

    [TestMethod]
    public async Task Edit_Post_ScopeDeniesAccess_RedirectsToEditSelf()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);
        var model = new DiscountModel { Id = "1" };

        var result = await _sut.Edit(model, false) as RedirectToActionResult;

        Assert.AreEqual("Edit", result!.ActionName);
    }

    [TestMethod]
    public async Task Delete_ScopeDeniesAccess_RedirectsToEditSelf()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);
        _mediator.Setup(x => x.Send(It.IsAny<GetDiscountUsageHistoryQuery>(), default))
            .ReturnsAsync(new PagedList<DiscountUsageHistory>());

        var result = await _sut.Delete("1") as RedirectToActionResult;

        Assert.AreEqual("Edit", result!.ActionName);
    }

    [TestMethod]
    public async Task Delete_HasUsageHistory_BlocksDeletionEvenWithAccess()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(true);
        _mediator.Setup(x => x.Send(It.IsAny<GetDiscountUsageHistoryQuery>(), default))
            .ReturnsAsync(new PagedList<DiscountUsageHistory>(new List<DiscountUsageHistory> { new() }, 0, int.MaxValue));

        var result = await _sut.Delete("1") as RedirectToActionResult;

        Assert.AreEqual("Edit", result!.ActionName);
        _vmService.Verify(x => x.DeleteDiscount(It.IsAny<Discount>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_HasAccessAndNoUsageHistory_DeletesAndRedirectsToList()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(true);
        _mediator.Setup(x => x.Send(It.IsAny<GetDiscountUsageHistoryQuery>(), default))
            .ReturnsAsync(new PagedList<DiscountUsageHistory>());

        var result = await _sut.Delete("1") as RedirectToActionResult;

        Assert.AreEqual("List", result!.ActionName);
        _vmService.Verify(x => x.DeleteDiscount(discount), Times.Once);
    }

    [TestMethod]
    public void Index_RedirectsToList()
    {
        var result = _sut.Index() as RedirectToActionResult;
        Assert.AreEqual("List", result!.ActionName);
    }

    [TestMethod]
    public async Task Create_Post_DefaultStoreIdSet_ForcesModelStores()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns("store-A");
        var model = new DiscountModel { Name = "Test" };
        _vmService.Setup(x => x.InsertDiscountModel(model)).ReturnsAsync(new Discount { Id = "1" });

        await _sut.Create(model, false);

        CollectionAssert.AreEqual(new[] { "store-A" }, model.Stores);
    }

    [TestMethod]
    public async Task CouponCodeDelete_ScopeDeniesAccess_ReturnsAccessDeniedJson()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);

        var result = await _sut.CouponCodeDelete("1", "coupon-1") as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual("Access denied", data.Errors);
    }

    [TestMethod]
    public async Task CouponCodeInsert_ScopeAllowsAccess_InsertsCoupon()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(true);
        _service.Setup(x => x.GetDiscountByCouponCode("SAVE10")).ReturnsAsync((Discount?)null);

        await _sut.CouponCodeInsert("1", "save10");

        _vmService.Verify(x => x.InsertCouponCode("1", "SAVE10"), Times.Once);
    }
}
