using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Web.Common.RequestCustomerResolvers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Wangkanai.Detection.Services;

namespace Grand.Web.Common.Tests;

[TestClass]
public class RequestCustomerResolversTests
{
    [TestMethod]
    public async Task AllowAnonymous_NoEndpoint_ReturnsNull()
    {
        var accessor = AccessorWith(new DefaultHttpContext());
        var customers = new Mock<ICustomerService>();

        var resolver = new AllowAnonymousCustomerResolver(accessor, customers.Object);

        Assert.IsNull(await resolver.Resolve());
        Assert.AreEqual(RequestCustomerResolverOrder.AllowAnonymous, resolver.Order);
    }

    [TestMethod]
    public async Task AllowAnonymous_NoHttpContext_ReturnsNull()
    {
        var resolver = new AllowAnonymousCustomerResolver(AccessorWith(null), Mock.Of<ICustomerService>());

        Assert.IsNull(await resolver.Resolve());
    }

    [TestMethod]
    public async Task AllowAnonymous_EndpointWithMetadata_ReturnsAnonymousCustomer()
    {
        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(new AllowAnonymousAttribute()), "t"));
        var customers = new Mock<ICustomerService>();
        customers.Setup(c => c.GetCustomerBySystemName(SystemCustomerNames.Anonymous))
            .ReturnsAsync(new Customer { Id = "anonymous" });

        var resolver = new AllowAnonymousCustomerResolver(AccessorWith(context), customers.Object);

        Assert.AreEqual("anonymous", (await resolver.Resolve()).Id);
    }

    [TestMethod]
    public async Task Cookie_NotSignedIn_ReturnsNull()
    {
        var authentication = new Mock<IGrandAuthenticationService>();
        var resolver = new CookieCustomerResolver(authentication.Object);

        Assert.IsNull(await resolver.Resolve());
        Assert.AreEqual(RequestCustomerResolverOrder.Cookie, resolver.Order);
    }

    [TestMethod]
    public async Task Cookie_SignedIn_ReturnsTheSignedInCustomer()
    {
        var authentication = new Mock<IGrandAuthenticationService>();
        authentication.Setup(a => a.GetAuthenticatedCustomer()).ReturnsAsync(new Customer { Id = "cookie" });

        var resolver = new CookieCustomerResolver(authentication.Object);

        Assert.AreEqual("cookie", (await resolver.Resolve()).Id);
    }

    [TestMethod]
    public async Task GuestCookie_NoGuid_ReturnsNull()
    {
        var authentication = new Mock<IGrandAuthenticationService>();
        var resolver = new GuestCookieCustomerResolver(authentication.Object, Mock.Of<ICustomerService>(),
            Mock.Of<IGroupService>());

        Assert.IsNull(await resolver.Resolve());
        Assert.AreEqual(RequestCustomerResolverOrder.GuestCookie, resolver.Order);
    }

    [TestMethod]
    public async Task GuestCookie_InvalidGuid_ReturnsNull()
    {
        var authentication = new Mock<IGrandAuthenticationService>();
        authentication.Setup(a => a.GetCustomerGuid()).ReturnsAsync("not-a-guid");
        var resolver = new GuestCookieCustomerResolver(authentication.Object, Mock.Of<ICustomerService>(),
            Mock.Of<IGroupService>());

        Assert.IsNull(await resolver.Resolve());
    }

    [TestMethod]
    public async Task GuestCookie_ActiveNonRegistered_ReturnsCustomer()
    {
        var guid = Guid.NewGuid();
        var guest = new Customer { Id = "guest", Active = true };
        var authentication = new Mock<IGrandAuthenticationService>();
        authentication.Setup(a => a.GetCustomerGuid()).ReturnsAsync(guid.ToString());
        var customers = new Mock<ICustomerService>();
        customers.Setup(c => c.GetCustomerByGuid(guid)).ReturnsAsync(guest);

        var resolver = new GuestCookieCustomerResolver(authentication.Object, customers.Object,
            Mock.Of<IGroupService>());

        Assert.AreSame(guest, await resolver.Resolve());
    }

    [TestMethod]
    public async Task GuestCookie_RegisteredDeletedOrInactive_ReturnsNull()
    {
        var guid = Guid.NewGuid();
        var authentication = new Mock<IGrandAuthenticationService>();
        authentication.Setup(a => a.GetCustomerGuid()).ReturnsAsync(guid.ToString());
        var customers = new Mock<ICustomerService>();
        var groups = new Mock<IGroupService>();
        var resolver = new GuestCookieCustomerResolver(authentication.Object, customers.Object, groups.Object);

        var registered = new Customer { Id = "r", Active = true };
        customers.Setup(c => c.GetCustomerByGuid(guid)).ReturnsAsync(registered);
        groups.Setup(g => g.IsRegistered(registered)).ReturnsAsync(true);
        Assert.IsNull(await resolver.Resolve());

        customers.Setup(c => c.GetCustomerByGuid(guid)).ReturnsAsync(new Customer { Active = true, Deleted = true });
        Assert.IsNull(await resolver.Resolve());

        customers.Setup(c => c.GetCustomerByGuid(guid)).ReturnsAsync(new Customer { Active = false });
        Assert.IsNull(await resolver.Resolve());
    }

    [TestMethod]
    public async Task SearchEngine_NotACrawler_ReturnsNull()
    {
        var detection = new Mock<IDetectionService>();
        var resolver = new SearchEngineCustomerResolver(detection.Object, Mock.Of<ICustomerService>());

        //Crawler is null: the null-safe read must not throw
        Assert.IsNull(await resolver.Resolve());
        Assert.AreEqual(RequestCustomerResolverOrder.SearchEngine, resolver.Order);

        var crawler = new Mock<ICrawlerService>();
        crawler.SetupGet(c => c.IsCrawler).Returns(false);
        detection.SetupGet(d => d.Crawler).Returns(crawler.Object);
        Assert.IsNull(await resolver.Resolve());
    }

    [TestMethod]
    public async Task SearchEngine_Crawler_ReturnsSearchEngineCustomer()
    {
        var crawler = new Mock<ICrawlerService>();
        crawler.SetupGet(c => c.IsCrawler).Returns(true);
        var detection = new Mock<IDetectionService>();
        detection.SetupGet(d => d.Crawler).Returns(crawler.Object);
        var customers = new Mock<ICustomerService>();
        customers.Setup(c => c.GetCustomerBySystemName(SystemCustomerNames.SearchEngine))
            .ReturnsAsync(new Customer { Id = "crawler" });

        var resolver = new SearchEngineCustomerResolver(detection.Object, customers.Object);

        Assert.AreEqual("crawler", (await resolver.Resolve()).Id);
    }

    [TestMethod]
    public async Task ApiUser_NoApiCustomer_ReturnsNull()
    {
        var api = new Mock<IApiAuthenticationService>();
        var resolver = new ApiUserCustomerResolver(api.Object, AccessorWith(new DefaultHttpContext()),
            Mock.Of<ICustomerService>());

        Assert.IsNull(await resolver.Resolve());
        Assert.AreEqual(RequestCustomerResolverOrder.ApiUser, resolver.Order);
    }

    [TestMethod]
    public async Task ApiUser_RejectedBearerToken_ReturnsAnonymous()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer expired";
        var customers = new Mock<ICustomerService>();
        customers.Setup(c => c.GetCustomerBySystemName(SystemCustomerNames.Anonymous))
            .ReturnsAsync(new Customer { Id = "anonymous" });

        var resolver = new ApiUserCustomerResolver(Mock.Of<IApiAuthenticationService>(), AccessorWith(context),
            customers.Object);

        Assert.AreEqual("anonymous", (await resolver.Resolve()).Id);
    }

    [TestMethod]
    public async Task ApiUser_RejectedBasicCredentials_ReturnsNull()
    {
        //a staging site behind basic auth: a new visitor still becomes a new guest
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Basic dXNlcjpwYXNz";
        var customers = new Mock<ICustomerService>();

        var resolver = new ApiUserCustomerResolver(Mock.Of<IApiAuthenticationService>(), AccessorWith(context),
            customers.Object);

        Assert.IsNull(await resolver.Resolve());
        customers.Verify(c => c.GetCustomerBySystemName(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ApiUser_Authenticated_ReturnsCustomer()
    {
        var api = new Mock<IApiAuthenticationService>();
        api.Setup(a => a.GetAuthenticatedCustomer()).ReturnsAsync(new Customer { Id = "api" });

        var resolver = new ApiUserCustomerResolver(api.Object, AccessorWith(new DefaultHttpContext()),
            Mock.Of<ICustomerService>());

        Assert.AreEqual("api", (await resolver.Resolve()).Id);
    }

    private static IHttpContextAccessor AccessorWith(HttpContext context)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(context);
        return accessor.Object;
    }
}
