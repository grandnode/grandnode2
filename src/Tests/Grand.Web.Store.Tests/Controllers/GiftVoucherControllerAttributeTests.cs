using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
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
public class GiftVoucherControllerAttributeTests
{
    [TestMethod]
    public void IsThinSubclassOfBaseGiftVoucherController()
    {
        Assert.IsTrue(typeof(BaseGiftVoucherController).IsAssignableFrom(typeof(GiftVoucherController)));
    }

    [TestMethod]
    public void HasAuthorizeStoreAttribute()
    {
        var attr = typeof(GiftVoucherController).GetCustomAttributes(typeof(AuthorizeStoreAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void HasAreaStoreAttribute()
    {
        var attr = typeof(GiftVoucherController)
            .GetCustomAttributes(typeof(AreaAttribute), inherit: false)
            .Cast<AreaAttribute>().Single();
        Assert.AreEqual("Store", attr.RouteValue);
    }

    [TestMethod]
    public void HasAuthorizeMenuAttribute()
    {
        var attr = typeof(GiftVoucherController).GetCustomAttributes(typeof(AuthorizeMenuAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void HasAutoValidateAntiforgeryTokenAttribute()
    {
        var attr = typeof(GiftVoucherController)
            .GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), inherit: true);
        Assert.AreEqual(1, attr.Length);
    }

    // --- EditWarningCheck ----------------------------------------------------------------------
    //
    // Behavioral tests for GiftVoucherController.EditWarningCheck, exercised indirectly through
    // the public Edit(GET) action since EditWarningCheck itself is protected. Mirrors
    // BrandControllerTests.EditWarningCheckTests's pattern.

    [TestClass]
    public class EditWarningCheckTests
    {
        private const string PermissionsResourceKey = "Admin.GiftVouchers.Permissions";
        private const string DefaultStoreId = "store-1";

        private GiftVoucherController _controller;
        private Mock<IGiftVoucherViewModelService> _viewModelServiceMock;
        private Mock<IGiftVoucherService> _giftVoucherServiceMock;
        private Mock<ITranslationService> _translationServiceMock;
        private Mock<IAdminDataScope<GiftVoucher>> _scopeMock;

        [TestInitialize]
        public void Setup()
        {
            _viewModelServiceMock = new Mock<IGiftVoucherViewModelService>();
            _viewModelServiceMock.Setup(s => s.PrepareGiftVoucherModel(It.IsAny<GiftVoucher>()))
                .ReturnsAsync(new Grand.Web.AdminShared.Models.Orders.GiftVoucherModel());
            _giftVoucherServiceMock = new Mock<IGiftVoucherService>();
            _translationServiceMock = new Mock<ITranslationService>();
            _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

            _scopeMock = new Mock<IAdminDataScope<GiftVoucher>>();
            _scopeMock.Setup(s => s.DefaultStoreId).Returns(DefaultStoreId);
            _scopeMock.Setup(s => s.CanView(It.IsAny<GiftVoucher>())).ReturnsAsync(true);

            _controller = new GiftVoucherController(
                _viewModelServiceMock.Object,
                _giftVoucherServiceMock.Object,
                _translationServiceMock.Object,
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
        public async Task EditGet_GlobalVoucher_RaisesPermissionsWarning()
        {
            var giftVoucher = new GiftVoucher { Id = "gv-1", StoreId = "" };
            _giftVoucherServiceMock.Setup(s => s.GetGiftVoucherById("gv-1")).ReturnsAsync(giftVoucher);

            await _controller.Edit("gv-1");

            Assert.IsTrue(WarningWasRaised());
            _translationServiceMock.Verify(t => t.GetResource(PermissionsResourceKey), Times.Once);
        }

        [TestMethod]
        public async Task EditGet_OwnStoreVoucher_DoesNotRaiseWarning()
        {
            var giftVoucher = new GiftVoucher { Id = "gv-1", StoreId = DefaultStoreId };
            _giftVoucherServiceMock.Setup(s => s.GetGiftVoucherById("gv-1")).ReturnsAsync(giftVoucher);

            await _controller.Edit("gv-1");

            Assert.IsFalse(WarningWasRaised());
            _translationServiceMock.Verify(t => t.GetResource(PermissionsResourceKey), Times.Never);
        }
    }
}
