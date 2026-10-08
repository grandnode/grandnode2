using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests;

[TestClass]
public class WorkContextSetterRequestResolverTests
{
    private Mock<ICustomerService> _customerService;
    private DefaultHttpContext _httpContext;
    private Mock<IGrandAuthenticationService> _authentication;

    [TestInitialize]
    public void Setup()
    {
        _customerService = new Mock<ICustomerService>();
        _authentication = new Mock<IGrandAuthenticationService>();
        _httpContext = new DefaultHttpContext();
    }

    private TestableSetter CreateSetter(params IRequestCustomerResolver[] resolvers)
    {
        var services = new ServiceCollection();
        foreach (var resolver in resolvers)
            services.AddSingleton(resolver);
        _httpContext.RequestServices = services.BuildServiceProvider();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(_httpContext);
        return new TestableSetter(accessor.Object, _authentication.Object, Mock.Of<ICurrencyService>(),
            _customerService.Object, Mock.Of<IGroupService>(), Mock.Of<ILanguageService>(),
            Mock.Of<IStoreService>(), Mock.Of<IAclService>(), Mock.Of<IVendorService>(),
            new TaxSettings(), new AppConfig());
    }

    private static IRequestCustomerResolver Resolver(int order, Customer result)
    {
        var mock = new Mock<IRequestCustomerResolver>();
        mock.SetupGet(r => r.Order).Returns(order);
        mock.Setup(r => r.Resolve(It.IsAny<HttpContext>(), It.IsAny<Store>())).ReturnsAsync(result);
        return mock.Object;
    }

    [TestMethod]
    public async Task ResolverCustomer_WinsOverCookie()
    {
        _authentication.Setup(a => a.GetAuthenticatedCustomer()).ReturnsAsync(new Customer { Id = "cookie" });
        var setter = CreateSetter(Resolver(10, new Customer { Id = "resolved" }));

        var customer = await setter.CurrentCustomerForTest(new Store());

        Assert.AreEqual("resolved", customer.Id);
    }

    [TestMethod]
    public async Task Resolvers_AskedInOrder_FirstNonNullWins()
    {
        var setter = CreateSetter(Resolver(20, new Customer { Id = "second" }), Resolver(5, null),
            Resolver(10, new Customer { Id = "first" }));

        var customer = await setter.CurrentCustomerForTest(new Store());

        Assert.AreEqual("first", customer.Id);
    }

    [TestMethod]
    public async Task AllResolversNull_FallsThroughToCookie()
    {
        _authentication.Setup(a => a.GetAuthenticatedCustomer()).ReturnsAsync(new Customer { Id = "cookie" });
        var setter = CreateSetter(Resolver(10, null));

        var customer = await setter.CurrentCustomerForTest(new Store());

        Assert.AreEqual("cookie", customer.Id);
    }

    [TestMethod]
    public async Task NoResolverRegistered_FallsThroughToCookie()
    {
        _authentication.Setup(a => a.GetAuthenticatedCustomer()).ReturnsAsync(new Customer { Id = "cookie" });
        var setter = CreateSetter();

        var customer = await setter.CurrentCustomerForTest(new Store());

        Assert.AreEqual("cookie", customer.Id);
    }

    [TestMethod]
    public async Task NoRequestServices_FallsThroughToCookie()
    {
        _authentication.Setup(a => a.GetAuthenticatedCustomer()).ReturnsAsync(new Customer { Id = "cookie" });
        var setter = CreateSetter();
        _httpContext.RequestServices = null;

        var customer = await setter.CurrentCustomerForTest(new Store());

        Assert.AreEqual("cookie", customer.Id);
    }

    private class TestableSetter(
        IHttpContextAccessor httpContextAccessor, IGrandAuthenticationService authenticationService,
        ICurrencyService currencyService, ICustomerService customerService, IGroupService groupService,
        ILanguageService languageService, IStoreService storeService, IAclService aclService,
        IVendorService vendorService, TaxSettings taxSettings, AppConfig config)
        : WorkContextSetter(httpContextAccessor, authenticationService, currencyService, customerService,
            groupService, languageService, storeService, aclService, vendorService, taxSettings, config)
    {
        public Task<Customer> CurrentCustomerForTest(Store store) => CurrentCustomer(store);
    }
}
