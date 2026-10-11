using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Web.Common.Middleware;
using Grand.Web.Common.Security.Oidc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Middleware;

[TestClass]
public class ContextMiddlewareTests
{
    private Mock<IStoreContextSetter> _storeContextSetter;
    private Mock<IWorkContextSetter> _workContextSetter;

    private async Task<bool> Invoke(string routePattern, Action<HttpContext> request = null)
    {
        _storeContextSetter = new Mock<IStoreContextSetter>();
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(x => x.CurrentStore).Returns(new Store { Id = "store1" });
        _storeContextSetter.Setup(x => x.InitializeStoreContext(It.IsAny<string>())).ReturnsAsync(storeContext.Object);
        _workContextSetter = new Mock<IWorkContextSetter>();

        var services = new ServiceCollection();
        services.AddSingleton(_storeContextSetter.Object);
        services.AddSingleton(_workContextSetter.Object);
        var http = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        http.SetEndpoint(new RouteEndpoint(_ => Task.CompletedTask, RoutePatternFactory.Parse(routePattern), 0,
            EndpointMetadataCollection.Empty, null));
        request?.Invoke(http);

        var nextCalled = false;
        var middleware = new ContextMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.SetupAllProperties();
        await middleware.InvokeAsync(http, contextAccessor.Object);
        return nextCalled;
    }

    [TestMethod]
    [DataRow(OidcProtectedResourceMetadata.WellKnownPath)]
    [DataRow(OidcProtectedResourceMetadata.WellKnownPath + "/{**path}")]
    public async Task ProtectedResourceMetadata_NoStoreOrWorkContext(string routePattern)
    {
        Assert.IsTrue(await Invoke(routePattern));
        _storeContextSetter.Verify(x => x.InitializeStoreContext(It.IsAny<string>()), Times.Never);
        _workContextSetter.Verify(x => x.InitializeWorkContext(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task OtherRoute_InitializesStoreAndWorkContext()
    {
        Assert.IsTrue(await Invoke("/api/{controller}"));
        _workContextSetter.Verify(x => x.InitializeWorkContext("store1"), Times.Once);
    }

    [TestMethod]
    public async Task NoPinnedStore_StoreContextFromCookieOrHost()
    {
        Assert.IsTrue(await Invoke("/api/{controller}"));
        _storeContextSetter.Verify(x => x.InitializeStoreContext(null), Times.Once);
    }

    [TestMethod]
    public async Task PinnedStore_PassedToTheStoreContext()
    {
        Assert.IsTrue(await Invoke("/api/{controller}", http => RequestStorePin.Set(http, "store2")));
        _storeContextSetter.Verify(x => x.InitializeStoreContext("store2"), Times.Once);
    }
}
