using Grand.Business.Core.Interfaces.Cms;
using Grand.Domain.Pages;
using Grand.Infrastructure;
using Grand.Infrastructure.Security;
using Grand.Web.Vendor.Components;
using Grand.Web.Vendor.Models.Common;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Vendor.Tests.Components;

[TestClass]
public class VendorPageViewComponentTests
{
    private const string SystemName = "VendorPortalInfo";
    private const string StoreId = "store-1";

    private Mock<IPageService> _pageServiceMock = null!;
    private Mock<IHtmlSanitizationService> _sanitizerMock = null!;
    private VendorPageViewComponent _component = null!;

    [TestInitialize]
    public void Init()
    {
        _pageServiceMock = new Mock<IPageService>();
        _sanitizerMock = new Mock<IHtmlSanitizationService>();

        var storeContextMock = new Mock<IStoreContext>();
        storeContextMock.Setup(x => x.CurrentStore).Returns(new Grand.Domain.Stores.Store { Id = StoreId });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(x => x.StoreContext).Returns(storeContextMock.Object);

        _component = new VendorPageViewComponent(_pageServiceMock.Object, contextAccessorMock.Object,
            _sanitizerMock.Object, NullLogger<VendorPageViewComponent>.Instance);
    }

    private async Task<VendorPortalModel> Invoke(Page page)
    {
        _pageServiceMock.Setup(x => x.GetPageBySystemName(SystemName, StoreId)).ReturnsAsync(page);
        var result = await _component.InvokeAsync(SystemName);
        return (VendorPortalModel)((ViewViewComponentResult)result).ViewData!.Model!;
    }

    [TestMethod]
    public async Task InvokeAsync_BodyInsideAllowlist_RendersTitleAndBody()
    {
        _sanitizerMock.Setup(x => x.ContainsDisallowedRichText("<p>Hello</p>")).Returns(false);

        var model = await Invoke(new Page { Title = "Welcome", Body = "<p>Hello</p>" });

        Assert.AreEqual("Welcome", model.Title);
        Assert.AreEqual("<p>Hello</p>", model.Body);
    }

    [TestMethod]
    public async Task InvokeAsync_BodyOutsideAllowlist_BodyIsNotRendered()
    {
        _sanitizerMock.Setup(x => x.ContainsDisallowedRichText(It.IsAny<string>())).Returns(true);

        var model = await Invoke(new Page { Title = "Welcome", Body = "<img src=x onerror=alert(1)>" });

        Assert.AreEqual("Welcome", model.Title);
        Assert.IsNull(model.Body);
    }

    [TestMethod]
    public async Task InvokeAsync_PageMissing_RendersEmptyModel_WithoutCheckingBody()
    {
        var model = await Invoke(null);

        Assert.IsNull(model.Title);
        Assert.IsNull(model.Body);
        _sanitizerMock.Verify(x => x.ContainsDisallowedRichText(It.IsAny<string>()), Times.Never);
    }
}
