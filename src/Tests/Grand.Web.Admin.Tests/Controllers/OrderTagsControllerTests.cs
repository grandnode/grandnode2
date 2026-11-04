using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Models.Orders;
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

[TestClass]
public class OrderTagsControllerTests
{
    private OrderTagsController _controller;
    private Mock<IOrderTagService> _orderTagServiceMock;
    private Mock<IRequestTranslationService> _translationServiceMock;

    [TestInitialize]
    public void Setup()
    {
        _orderTagServiceMock = new Mock<IOrderTagService>();
        _translationServiceMock = new Mock<IRequestTranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _controller = new OrderTagsController(
            _orderTagServiceMock.Object,
            new Mock<IOrderService>().Object,
            new Mock<IRequestPermissionService>().Object,
            _translationServiceMock.Object);

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

    [TestMethod]
    public async Task EditPost_ValidModel_SavesAndRedirectsToEditWithSuccess()
    {
        var tag = new OrderTag { Id = "t1", Name = "old" };
        _orderTagServiceMock.Setup(s => s.GetOrderTagById("t1")).ReturnsAsync(tag);

        var result = await _controller.Edit(new OrderTagModel { Id = "t1", Name = "new" });

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        Assert.AreEqual("t1", redirect.RouteValues["id"]);
        _orderTagServiceMock.Verify(s => s.UpdateOrderTag(It.Is<OrderTag>(t => t.Name == "new")), Times.Once);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Orders.OrderTags.Updated"), Times.Once);
    }

    [TestMethod]
    public async Task EditPost_InvalidModel_RedisplaysTheFormWithoutSaving()
    {
        _orderTagServiceMock.Setup(s => s.GetOrderTagById("t1")).ReturnsAsync(new OrderTag { Id = "t1", Name = "old" });
        _controller.ModelState.AddModelError("Name", "required");

        var result = await _controller.Edit(new OrderTagModel { Id = "t1" });

        Assert.IsInstanceOfType<ViewResult>(result);
        _orderTagServiceMock.Verify(s => s.UpdateOrderTag(It.IsAny<OrderTag>()), Times.Never);
    }

    [TestMethod]
    public async Task EditPost_TagNotFound_RedirectsToListWithoutSaving()
    {
        _orderTagServiceMock.Setup(s => s.GetOrderTagById("missing")).ReturnsAsync((OrderTag)null);

        var result = await _controller.Edit(new OrderTagModel { Id = "missing", Name = "new" });

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _orderTagServiceMock.Verify(s => s.UpdateOrderTag(It.IsAny<OrderTag>()), Times.Never);
    }
}
