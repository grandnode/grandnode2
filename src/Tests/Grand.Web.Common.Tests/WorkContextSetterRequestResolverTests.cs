using System.Reflection;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.RequestCustomerResolvers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Wangkanai.Detection.Services;

namespace Grand.Web.Common.Tests;

[TestClass]
public class WorkContextSetterRequestResolverTests
{
    private Mock<ICustomerService> _customerService;
    private Mock<IGroupService> _groupService;
    private DefaultHttpContext _httpContext;
    private Mock<IGrandAuthenticationService> _authentication;
    private Mock<IApiAuthenticationService> _apiAuthentication;
    private Mock<IDetectionService> _detection;
    private Mock<IHttpContextAccessor> _accessor;

    [TestInitialize]
    public void Setup()
    {
        _customerService = new Mock<ICustomerService>();
        _groupService = new Mock<IGroupService>();
        _authentication = new Mock<IGrandAuthenticationService>();
        _apiAuthentication = new Mock<IApiAuthenticationService>();
        _detection = new Mock<IDetectionService>();
        _httpContext = new DefaultHttpContext();
        _accessor = new Mock<IHttpContextAccessor>();
        _accessor.Setup(a => a.HttpContext).Returns(_httpContext);
    }

    private TestableSetter CreateSetter(params IRequestCustomerResolver[] resolvers)
    {
        var services = new ServiceCollection();
        foreach (var resolver in resolvers)
            services.AddSingleton(resolver);
        return new TestableSetter(_accessor.Object, _authentication.Object, Mock.Of<ICurrencyService>(),
            _customerService.Object, Mock.Of<ILanguageService>(),
            Mock.Of<IStoreService>(), Mock.Of<IAclService>(), Mock.Of<IVendorService>(),
            new TaxSettings(), new AppConfig(), services.BuildServiceProvider());
    }

    private static Mock<IRequestCustomerResolver> ResolverMock(int order, Customer result)
    {
        var mock = new Mock<IRequestCustomerResolver>();
        mock.SetupGet(r => r.Order).Returns(order);
        mock.Setup(r => r.Resolve()).ReturnsAsync(result);
        return mock;
    }

    //registered in a scrambled order on purpose: the setter must sort by Order
    private IRequestCustomerResolver[] BuiltIns()
    {
        return [
            new ApiUserCustomerResolver(_apiAuthentication.Object, _accessor.Object, _customerService.Object),
            new SearchEngineCustomerResolver(_detection.Object, _customerService.Object),
            new GuestCookieCustomerResolver(_authentication.Object, _customerService.Object, _groupService.Object),
            new CookieCustomerResolver(_authentication.Object),
            new AllowAnonymousCustomerResolver(_accessor.Object, _customerService.Object)
        ];
    }

    private void ArrangeAllBuiltInsMatch()
    {
        var endpoint = new Endpoint(null, new EndpointMetadataCollection(new AllowAnonymousAttribute()), "test");
        _httpContext.SetEndpoint(endpoint);
        _customerService.Setup(c => c.GetCustomerBySystemName(SystemCustomerNames.Anonymous))
            .ReturnsAsync(new Customer { Id = "anonymous" });
        _authentication.Setup(a => a.GetAuthenticatedCustomer()).ReturnsAsync(new Customer { Id = "cookie" });
        var guid = Guid.NewGuid();
        _authentication.Setup(a => a.GetCustomerGuid()).ReturnsAsync(guid.ToString());
        _customerService.Setup(c => c.GetCustomerByGuid(guid))
            .ReturnsAsync(new Customer { Id = "guest", Active = true });
        var crawler = new Mock<ICrawlerService>();
        crawler.SetupGet(c => c.IsCrawler).Returns(true);
        _detection.SetupGet(d => d.Crawler).Returns(crawler.Object);
        _customerService.Setup(c => c.GetCustomerBySystemName(SystemCustomerNames.SearchEngine))
            .ReturnsAsync(new Customer { Id = "crawler" });
        _apiAuthentication.Setup(a => a.GetAuthenticatedCustomer()).ReturnsAsync(new Customer { Id = "api" });
    }

    [TestMethod]
    public async Task BuiltIns_AllowAnonymousEndpoint_BeatsValidCookie()
    {
        ArrangeAllBuiltInsMatch();
        var customer = await CreateSetter(BuiltIns()).CurrentCustomerForTest(new Store());
        Assert.AreEqual("anonymous", customer.Id);
    }

    [TestMethod]
    public async Task BuiltIns_Cookie_BeatsGuestCookie()
    {
        ArrangeAllBuiltInsMatch();
        _httpContext.SetEndpoint(null);
        var customer = await CreateSetter(BuiltIns()).CurrentCustomerForTest(new Store());
        Assert.AreEqual("cookie", customer.Id);
    }

    [TestMethod]
    public async Task BuiltIns_GuestCookie_BeatsSearchEngine()
    {
        ArrangeAllBuiltInsMatch();
        _httpContext.SetEndpoint(null);
        _authentication.Setup(a => a.GetAuthenticatedCustomer()).ReturnsAsync((Customer)null);
        var customer = await CreateSetter(BuiltIns()).CurrentCustomerForTest(new Store());
        Assert.AreEqual("guest", customer.Id);
    }

    [TestMethod]
    public async Task BuiltIns_SearchEngine_BeatsApiUser()
    {
        ArrangeAllBuiltInsMatch();
        _httpContext.SetEndpoint(null);
        _authentication.Setup(a => a.GetAuthenticatedCustomer()).ReturnsAsync((Customer)null);
        _authentication.Setup(a => a.GetCustomerGuid()).ReturnsAsync((string)null);
        var customer = await CreateSetter(BuiltIns()).CurrentCustomerForTest(new Store());
        Assert.AreEqual("crawler", customer.Id);
    }

    [TestMethod]
    public async Task BuiltIns_ApiUser_WinsWhenNothingElseMatches()
    {
        ArrangeAllBuiltInsMatch();
        _httpContext.SetEndpoint(null);
        _authentication.Setup(a => a.GetAuthenticatedCustomer()).ReturnsAsync((Customer)null);
        _authentication.Setup(a => a.GetCustomerGuid()).ReturnsAsync((string)null);
        _detection.SetupGet(d => d.Crawler).Returns((ICrawlerService)null);
        var customer = await CreateSetter(BuiltIns()).CurrentCustomerForTest(new Store());
        Assert.AreEqual("api", customer.Id);
    }

    [TestMethod]
    public async Task BuiltIns_NothingMatches_CreatesNewGuest()
    {
        _authentication.Setup(a => a.GetAuthenticatedCustomer()).ReturnsAsync((Customer)null);
        _authentication.Setup(a => a.GetCustomerGuid()).ReturnsAsync((string)null);
        _detection.SetupGet(d => d.Crawler).Returns((ICrawlerService)null);
        _apiAuthentication.Setup(a => a.GetAuthenticatedCustomer()).ReturnsAsync((Customer)null);
        _customerService.Setup(c => c.InsertGuestCustomer(It.IsAny<Customer>()))
            .ReturnsAsync((Customer c) => c);

        var customer = await CreateSetter(BuiltIns()).CurrentCustomerForTest(new Store { Id = "s1" });

        Assert.IsNotNull(customer);
        Assert.AreEqual("s1", customer.StoreId);
        _authentication.Verify(a => a.SetCustomerGuid(customer.CustomerGuid), Times.Once);
    }

    [TestMethod]
    public async Task BackgroundTask_NoHttpContext_ResolversAreNotAsked()
    {
        _accessor.Setup(a => a.HttpContext).Returns((HttpContext)null);
        _customerService.Setup(c => c.GetCustomerBySystemName(SystemCustomerNames.BackgroundTask))
            .ReturnsAsync(new Customer { Id = "task" });
        var resolver = ResolverMock(10, new Customer { Id = "resolved" });

        var customer = await CreateSetter(resolver.Object).CurrentCustomerForTest(new Store());

        Assert.AreEqual("task", customer.Id);
        resolver.Verify(r => r.Resolve(), Times.Never);
    }

    [TestMethod]
    public async Task BackgroundTask_FromContainer_ResolversAreNotConstructed()
    {
        //the real registration: resolvers needing the request (detection, API authentication) must not be built
        //in a scope without one, so a resolver that cannot be constructed there must not fail the task
        _accessor.Setup(a => a.HttpContext).Returns((HttpContext)null);
        _customerService.Setup(c => c.GetCustomerBySystemName(SystemCustomerNames.BackgroundTask))
            .ReturnsAsync(new Customer { Id = "task" });
        var services = new ServiceCollection();
        services.AddSingleton(_accessor.Object);
        services.AddSingleton(_authentication.Object);
        services.AddSingleton(Mock.Of<ICurrencyService>());
        services.AddSingleton(_customerService.Object);
        services.AddSingleton(Mock.Of<ILanguageService>());
        services.AddSingleton(Mock.Of<IStoreService>());
        services.AddSingleton(Mock.Of<IAclService>());
        services.AddSingleton(Mock.Of<IVendorService>());
        services.AddSingleton(new TaxSettings());
        services.AddSingleton(new AppConfig());
        services.AddScoped<TestableSetter>();
        services.AddRequestCustomerResolvers();
        services.AddScoped<IRequestCustomerResolver, UnconstructibleResolver>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var customer = await scope.ServiceProvider.GetRequiredService<TestableSetter>()
            .CurrentCustomerForTest(new Store());

        Assert.AreEqual("task", customer.Id);
    }

    [TestMethod]
    public async Task Resolver_Throws_IsSkippedAndNextResolverAsked()
    {
        var failing = new Mock<IRequestCustomerResolver>();
        failing.SetupGet(r => r.Order).Returns(10);
        failing.Setup(r => r.Resolve()).ThrowsAsync(new InvalidOperationException("idp down"));
        var next = ResolverMock(20, new Customer { Id = "next" });

        var customer = await CreateSetter(failing.Object, next.Object).CurrentCustomerForTest(new Store());

        Assert.AreEqual("next", customer.Id);
    }

    [TestMethod]
    public async Task CustomResolver_BetweenBuiltIns_UsesItsOrder()
    {
        ArrangeAllBuiltInsMatch();
        _httpContext.SetEndpoint(null);
        _authentication.Setup(a => a.GetAuthenticatedCustomer()).ReturnsAsync((Customer)null);
        //between Cookie (2000) and GuestCookie (3000)
        var custom = ResolverMock(2500, new Customer { Id = "custom" });
        var all = BuiltIns().Append(custom.Object).ToArray();

        var customer = await CreateSetter(all).CurrentCustomerForTest(new Store());

        Assert.AreEqual("custom", customer.Id);
    }

    [TestMethod]
    public async Task CustomResolver_BeforeAllowAnonymous_Wins()
    {
        ArrangeAllBuiltInsMatch();
        var custom = ResolverMock(100, new Customer { Id = "custom" });
        var all = BuiltIns().Append(custom.Object).ToArray();

        var customer = await CreateSetter(all).CurrentCustomerForTest(new Store());

        Assert.AreEqual("custom", customer.Id);
    }

    [TestMethod]
    public async Task Resolvers_AskedInOrder_FirstNonNullWins()
    {
        var second = ResolverMock(20, new Customer { Id = "second" });
        var first = ResolverMock(10, new Customer { Id = "first" });
        var skipped = ResolverMock(5, null);

        var customer = await CreateSetter(second.Object, skipped.Object, first.Object)
            .CurrentCustomerForTest(new Store());

        Assert.AreEqual("first", customer.Id);
        skipped.Verify(r => r.Resolve(), Times.Once);
        second.Verify(r => r.Resolve(), Times.Never);
    }

    [TestMethod]
    public async Task Impersonation_AppliedToSignedInCustomer()
    {
        var signedIn = new Customer { Id = "admin" };
        signedIn.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.ImpersonatedCustomerId, Value = "target", StoreId = "" });
        var target = new Customer { Id = "target", Active = true };
        _customerService.Setup(c => c.GetCustomerById("target")).ReturnsAsync(target);
        _authentication.Setup(a => a.GetAuthenticatedCustomer()).ReturnsAsync(signedIn);
        var setter = CreateSetter(new CookieCustomerResolver(_authentication.Object));

        var customer = await setter.CurrentCustomerForTest(new Store());

        Assert.AreEqual("target", customer.Id);
        Assert.AreSame(signedIn, setter.OriginalCustomerForTest);
    }

    [TestMethod]
    public async Task Impersonation_NotAppliedToCustomerOfAnotherResolver()
    {
        var other = new Customer { Id = "other" };
        other.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.ImpersonatedCustomerId, Value = "target", StoreId = "" });
        _customerService.Setup(c => c.GetCustomerById("target"))
            .ReturnsAsync(new Customer { Id = "target", Active = true });
        var setter = CreateSetter(ResolverMock(10, other).Object);

        var customer = await setter.CurrentCustomerForTest(new Store());

        Assert.AreEqual("other", customer.Id);
        Assert.IsNull(setter.OriginalCustomerForTest);
        _customerService.Verify(c => c.GetCustomerById(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Impersonation_TargetInactive_KeepsSignedInCustomer()
    {
        var signedIn = new Customer { Id = "admin" };
        signedIn.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.ImpersonatedCustomerId, Value = "target", StoreId = "" });
        _customerService.Setup(c => c.GetCustomerById("target"))
            .ReturnsAsync(new Customer { Id = "target", Active = false });
        _authentication.Setup(a => a.GetAuthenticatedCustomer()).ReturnsAsync(signedIn);
        var setter = CreateSetter(new CookieCustomerResolver(_authentication.Object));

        var customer = await setter.CurrentCustomerForTest(new Store());

        Assert.AreEqual("admin", customer.Id);
        Assert.IsNull(setter.OriginalCustomerForTest);
    }

    [TestMethod]
    public void RequestCustomerResolverOrder_AscendingAndDistinct()
    {
        int[] values = [
            RequestCustomerResolverOrder.AllowAnonymous, RequestCustomerResolverOrder.Cookie,
            RequestCustomerResolverOrder.GuestCookie, RequestCustomerResolverOrder.SearchEngine,
            RequestCustomerResolverOrder.ApiUser
        ];
        CollectionAssert.AreEqual(values.OrderBy(x => x).ToArray(), values);
        Assert.AreEqual(values.Length, values.Distinct().Count());
        Assert.AreEqual(1000, RequestCustomerResolverOrder.AllowAnonymous);
        Assert.AreEqual(5000, RequestCustomerResolverOrder.ApiUser);
    }

    private class TestableSetter(
        IHttpContextAccessor httpContextAccessor, IGrandAuthenticationService authenticationService,
        ICurrencyService currencyService, ICustomerService customerService,
        ILanguageService languageService, IStoreService storeService, IAclService aclService,
        IVendorService vendorService, TaxSettings taxSettings, AppConfig config,
        IServiceProvider serviceProvider)
        : WorkContextSetter(httpContextAccessor, authenticationService, currencyService, customerService,
            languageService, storeService, aclService, vendorService, taxSettings, config, serviceProvider,
            NullLogger<WorkContextSetter>.Instance)
    {
        public Customer OriginalCustomerForTest =>
            (Customer)typeof(WorkContextSetter)
                .GetField("_originalCustomerIfImpersonated", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(this);

        public Task<Customer> CurrentCustomerForTest(Store store) => CurrentCustomer(store);
    }

    private class UnconstructibleResolver : IRequestCustomerResolver
    {
        public UnconstructibleResolver() => throw new InvalidOperationException("needs the request");

        public int Order => 1;

        public Task<Customer> Resolve() => Task.FromResult<Customer>(null);
    }
}
