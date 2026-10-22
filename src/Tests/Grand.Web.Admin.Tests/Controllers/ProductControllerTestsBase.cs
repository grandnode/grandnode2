using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

/// <summary>
///     Fixture shared by the tests of BaseProductController and the per-tab product controllers split out of it.
///     Each derived class only says how to build its controller from the mocks below.
/// </summary>
public abstract class ProductControllerTestsBase<TController> where TController : Controller
{
    protected TController _controller;
    protected Mock<IProductService> _productServiceMock;
    protected Mock<IProductViewModelService> _productViewModelServiceMock;
    protected Mock<IRequestTranslationService> _translationServiceMock;
    protected Mock<IAdminDataScope<Product>> _scopeMock;
    protected Mock<IRequestPermissionService> _permissionServiceMock;
    protected Mock<IProductReservationService> _productReservationServiceMock;
    protected Mock<IAuctionService> _auctionServiceMock;
    protected Mock<IInventoryManageService> _inventoryManageServiceMock;
    protected Mock<ILanguageService> _languageServiceMock;
    protected Mock<IDateTimeService> _dateTimeServiceMock;
    protected Mock<IEnumTranslationService> _enumTranslationServiceMock;

    protected abstract TController CreateController();

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ProductProfile>();
            cfg.AddProfile<ProductSpecificationProfile>();
        });
        AutoMapperConfig.Init(mapperConfig);

        _productServiceMock = new Mock<IProductService>();
        _productViewModelServiceMock = new Mock<IProductViewModelService>();
        _translationServiceMock = new Mock<IRequestTranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _scopeMock = new Mock<IAdminDataScope<Product>>();
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Admin");
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);

        _permissionServiceMock = new Mock<IRequestPermissionService>();
        _permissionServiceMock.Setup(p => p.Authorize(It.IsAny<Permission>())).ReturnsAsync(true);

        _languageServiceMock = new Mock<ILanguageService>();
        _languageServiceMock.Setup(l => l.GetAllLanguages(true, It.IsAny<string>())).ReturnsAsync(new List<Domain.Localization.Language>());

        _productReservationServiceMock = new Mock<IProductReservationService>();
        _auctionServiceMock = new Mock<IAuctionService>();
        _inventoryManageServiceMock = new Mock<IInventoryManageService>();
        _dateTimeServiceMock = new Mock<IDateTimeService>();
        _enumTranslationServiceMock = new Mock<IEnumTranslationService>();

        _controller = CreateController();

        var httpContext = new DefaultHttpContext();
        // Needed for actions whose catch (Exception) block calls Error(exc), which logs via
        // HttpContext.RequestServices.GetRequiredService<ILoggerFactory>() (see Export / Import).
        // Once RequestServices is non-null, ControllerBase.Url's own
        // HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>() call (used by every
        // RedirectToAction(action, controller) - e.g. GoToSku) stops being short-circuited by the
        // null-conditional it uses when RequestServices itself is null, so IUrlHelperFactory must resolve
        // too or those tests start throwing.
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
        urlHelperFactoryMock.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
            .Returns(new Mock<IUrlHelper>().Object);
        var requestServicesMock = new Mock<IServiceProvider>();
        requestServicesMock.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(loggerFactoryMock.Object);
        requestServicesMock.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactoryMock.Object);
        httpContext.RequestServices = requestServicesMock.Object;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }
}
