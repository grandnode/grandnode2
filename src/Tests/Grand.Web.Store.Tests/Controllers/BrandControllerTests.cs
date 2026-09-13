using System.Reflection;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class BrandControllerTests
{
    [TestMethod]
    public void BrandController_IsThinSubclassOfBaseBrandController()
    {
        Assert.IsTrue(typeof(BrandController).IsSubclassOf(typeof(BaseBrandController)));
    }

    [TestMethod]
    public void BrandController_HasRequiredHostAttributes()
    {
        var type = typeof(BrandController);
        Assert.IsTrue(type.IsDefined(typeof(AreaAttribute), inherit: false), "[Area] missing");
        Assert.IsTrue(type.IsDefined(typeof(AuthorizeStoreAttribute), inherit: false), "[AuthorizeStore] missing");
        Assert.IsTrue(type.IsDefined(typeof(AuthorizeMenuAttribute), inherit: false), "[AuthorizeMenu] missing");

        var area = type.GetCustomAttribute<AreaAttribute>()!;
        Assert.AreEqual("Store", area.RouteValue);
    }

    // --- EditWarningCheck ----------------------------------------------------------------------
    //
    // Characterization tests for BrandController.EditWarningCheck, the one hand-ported piece of
    // business logic in the Brand consolidation (ARCH-001 Phase 13). Exercised indirectly through
    // the public Edit(GET) action, since EditWarningCheck itself is protected.

    [TestClass]
    public class EditWarningCheckTests
    {
        private const string PermissionsResourceKey = "Admin.Catalog.Brands.Permissions";

        private BrandController _controller;
        private Mock<IBrandService> _brandServiceMock;
        private Mock<ITranslationService> _translationServiceMock;
        private Mock<IAdminDataScope<Brand>> _scopeMock;

        [TestInitialize]
        public void Setup()
        {
            var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<BrandProfile>());
            AutoMapperConfig.Init(mapperConfig);

            _brandServiceMock = new Mock<IBrandService>();
            _translationServiceMock = new Mock<ITranslationService>();
            _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

            _scopeMock = new Mock<IAdminDataScope<Brand>>();
            _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
            _scopeMock.Setup(s => s.CanView(It.IsAny<Brand>())).ReturnsAsync(true);

            var languageServiceMock = new Mock<ILanguageService>();
            languageServiceMock.Setup(l => l.GetAllLanguages(true, It.IsAny<string>()))
                .ReturnsAsync(new List<Grand.Domain.Localization.Language>());

            _controller = new BrandController(
                new Mock<IBrandViewModelService>().Object,
                _brandServiceMock.Object,
                new Mock<IStoreService>().Object,
                languageServiceMock.Object,
                _translationServiceMock.Object,
                new Mock<IPictureViewModelService>().Object,
                _scopeMock.Object);

            var httpContext = new DefaultHttpContext();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
            var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
            urlHelperFactoryMock.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>())).Returns(new Mock<IUrlHelper>().Object);
            var requestServicesMock = new Mock<IServiceProvider>();
            requestServicesMock.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(loggerFactoryMock.Object);
            requestServicesMock.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactoryMock.Object);
            httpContext.RequestServices = requestServicesMock.Object;
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
        }

        private bool WarningWasRaised()
        {
            return _controller.TempData["grand.notifications.Warning"] is List<string> warnings
                   && warnings.Contains("resource");
        }

        [TestMethod]
        public async Task EditGet_NotLimitedToStores_RaisesPermissionsWarning()
        {
            var brand = new Brand { Id = "b1", LimitedToStores = false, Stores = [] };
            _brandServiceMock.Setup(b => b.GetBrandById("b1")).ReturnsAsync(brand);

            await _controller.Edit("b1");

            Assert.IsTrue(WarningWasRaised());
            _translationServiceMock.Verify(t => t.GetResource(PermissionsResourceKey), Times.Once);
        }

        [TestMethod]
        public async Task EditGet_LimitedToStores_ContainsDefaultStore_MultipleStores_RaisesPermissionsWarning()
        {
            var brand = new Brand { Id = "b1", LimitedToStores = true, Stores = ["store-1", "store-2"] };
            _brandServiceMock.Setup(b => b.GetBrandById("b1")).ReturnsAsync(brand);

            await _controller.Edit("b1");

            Assert.IsTrue(WarningWasRaised());
            _translationServiceMock.Verify(t => t.GetResource(PermissionsResourceKey), Times.Once);
        }

        [TestMethod]
        public async Task EditGet_LimitedToStores_ContainsDefaultStore_SingleStore_DoesNotRaiseWarning()
        {
            var brand = new Brand { Id = "b1", LimitedToStores = true, Stores = ["store-1"] };
            _brandServiceMock.Setup(b => b.GetBrandById("b1")).ReturnsAsync(brand);

            await _controller.Edit("b1");

            Assert.IsFalse(WarningWasRaised());
            _translationServiceMock.Verify(t => t.GetResource(PermissionsResourceKey), Times.Never);
        }
    }
}
