using Grand.Business.Core.Interfaces.Cms;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Grand.Web.Components;
using Grand.Web.Models.Cms;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Components;

[TestClass]
public class WidgetViewComponentTests
{
    private MemoryCacheBase _cacheBase;
    private Mock<IContextAccessor> _contextAccessorMock;
    private Mock<IWidgetService> _widgetServiceMock;

    [TestInitialize]
    public void Init()
    {
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(c => c.CurrentCustomer).Returns(new Customer());
        var storeContextMock = new Mock<IStoreContext>();
        storeContextMock.Setup(c => c.CurrentStore).Returns(new Domain.Stores.Store { Id = "store-1" });
        _contextAccessorMock = new Mock<IContextAccessor>();
        _contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);
        _contextAccessorMock.Setup(c => c.StoreContext).Returns(storeContextMock.Object);

        var widgetMock = new Mock<IWidgetProvider>();
        widgetMock.Setup(w => w.GetPublicViewComponentName(It.IsAny<string>())).ReturnsAsync("SomeWidget");
        _widgetServiceMock = new Mock<IWidgetService>();
        _widgetServiceMock.Setup(s => s.LoadActiveWidgetsByWidgetZone(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Customer>()))
            .ReturnsAsync([widgetMock.Object]);

        _cacheBase = new MemoryCacheBase(new MemoryCache(new MemoryCacheOptions()), new Mock<IMediator>().Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
    }

    private async Task<List<WidgetModel>> Invoke(object additionalData)
    {
        var component = new WidgetViewComponent(_contextAccessorMock.Object, _cacheBase, _widgetServiceMock.Object);
        var result = await component.InvokeAsync("blogpost_page_inside_comment", additionalData);
        return (List<WidgetModel>)((ViewViewComponentResult)result).ViewData!.Model;
    }

    [TestMethod]
    public async Task InvokeAsync_LaterRequest_DoesNotChangeAdditionalDataOfEarlierRequest()
    {
        var first = await Invoke("post-1");
        var second = await Invoke("post-2");

        Assert.AreEqual("post-1", first.Single().AdditionalData);
        Assert.AreEqual("post-2", second.Single().AdditionalData);
    }

    [TestMethod]
    public async Task InvokeAsync_WithoutAdditionalData_DoesNotReuseDataOfEarlierRequest()
    {
        await Invoke("post-1");

        var model = await Invoke(null);

        Assert.IsNull(model.Single().AdditionalData);
    }
}
