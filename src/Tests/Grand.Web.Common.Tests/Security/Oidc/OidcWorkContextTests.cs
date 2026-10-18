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
using Grand.Web.Common.Security.Authentication;
using Grand.Web.Common.Security.Oidc;
using Grand.Web.Common.UserAgent;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Claims;

namespace Grand.Web.Common.Tests.Security.Oidc;

/// <summary>
///     The OIDC request customer resolver in the work context, next to the built-in resolvers
/// </summary>
[TestClass]
public class OidcWorkContextTests
{
    private Mock<ICustomerService> _customerService;
    private Mock<IGrandAuthenticationService> _authentication;
    private Mock<IApiAuthenticationService> _apiAuthentication;
    private Mock<IAuthenticationService> _aspNetAuthentication;
    private Mock<IOidcProtectedResourceRegistry> _registry;
    private DefaultHttpContext _httpContext;
    private Mock<IHttpContextAccessor> _accessor;

    [TestInitialize]
    public void Setup()
    {
        _customerService = new Mock<ICustomerService>();
        _customerService.Setup(c => c.GetCustomerBySystemName(SystemCustomerNames.Anonymous))
            .ReturnsAsync(new Customer { Id = "anonymous", IsSystemAccount = true });
        _authentication = new Mock<IGrandAuthenticationService>();
        _authentication.Setup(a => a.GetAuthenticatedCustomer()).ReturnsAsync(new Customer { Id = "cookie", Active = true });
        _apiAuthentication = new Mock<IApiAuthenticationService>();
        _aspNetAuthentication = new Mock<IAuthenticationService>();
        _registry = new Mock<IOidcProtectedResourceRegistry>();
        _registry.Setup(r => r.Match(It.Is<PathString>(p => p.StartsWithSegments("/api"))))
            .Returns(new OidcProtectedResource { Name = "BackendApi", PathPrefix = "/api" });

        var requestServices = new ServiceCollection();
        requestServices.AddSingleton(_aspNetAuthentication.Object);
        _httpContext = new DefaultHttpContext { RequestServices = requestServices.BuildServiceProvider() };
        _httpContext.Request.Path = "/api/orders";
        _httpContext.Request.Headers.Authorization = "Bearer " + OidcTestHost.Token();
        _accessor = new Mock<IHttpContextAccessor>();
        _accessor.Setup(a => a.HttpContext).Returns(_httpContext);
    }

    private void TokenValidatedFor(Customer customer)
    {
        _aspNetAuthentication.Setup(a => a.AuthenticateAsync(_httpContext, OidcResourceServerDefaults.AuthenticationScheme))
            .ReturnsAsync(() =>
            {
                //what the Oidc JwtBearer events do once the token maps to a customer
                _httpContext.Items[OidcResourceServerDefaults.CustomerItemKey] = customer;
                return AuthenticateResult.Success(new AuthenticationTicket(
                    new ClaimsPrincipal(new ClaimsIdentity("Oidc")), OidcResourceServerDefaults.AuthenticationScheme));
            });
    }

    private void TokenRefused()
    {
        _aspNetAuthentication.Setup(a => a.AuthenticateAsync(_httpContext, OidcResourceServerDefaults.AuthenticationScheme))
            .ReturnsAsync(AuthenticateResult.Fail("The customer account is inactive"));
    }

    private TestableSetter CreateSetter()
    {
        var services = new ServiceCollection();
        //registered in a scrambled order on purpose: the setter sorts by Order
        services.AddSingleton<IRequestCustomerResolver>(
            new ApiUserCustomerResolver(_apiAuthentication.Object, _accessor.Object, _customerService.Object));
        services.AddSingleton<IRequestCustomerResolver>(new CookieCustomerResolver(_authentication.Object));
        services.AddSingleton<IRequestCustomerResolver>(
            new OidcRequestCustomerResolver(_accessor.Object, new OidcSchemeSelector(_registry.Object)));
        services.AddSingleton<IRequestCustomerResolver>(
            new GuestCookieCustomerResolver(_authentication.Object, _customerService.Object, Mock.Of<IGroupService>()));
        services.AddSingleton<IRequestCustomerResolver>(
            new SearchEngineCustomerResolver(Mock.Of<IUserAgentClassifier>(), _customerService.Object, _accessor.Object));
        services.AddSingleton<IRequestCustomerResolver>(
            new AllowAnonymousCustomerResolver(_accessor.Object, _customerService.Object));
        return new TestableSetter(_accessor.Object, _authentication.Object, Mock.Of<ICurrencyService>(),
            _customerService.Object, Mock.Of<ILanguageService>(), Mock.Of<IStoreService>(), Mock.Of<IAclService>(),
            Mock.Of<IVendorService>(), new TaxSettings(), new AppConfig(), services.BuildServiceProvider());
    }

    [TestMethod]
    public void Order_BeforeEveryBuiltInResolver()
    {
        var resolver = new OidcRequestCustomerResolver(_accessor.Object, new OidcSchemeSelector(_registry.Object));
        Assert.IsTrue(resolver.Order < RequestCustomerResolverOrder.AllowAnonymous);
    }

    [TestMethod]
    public async Task FollowsTheSchemeSelectorsDecisionForTheRequest()
    {
        //the authorization middleware already routed this request away from Oidc (a SecretKey token); the work
        //context must not read the token again and route it differently
        var selector = new OidcSchemeSelector(_registry.Object);
        _httpContext.Request.Headers.Authorization = "Bearer " + OidcTestHost.Token(
            key: new SymmetricSecurityKey(new byte[32]), algorithm: SecurityAlgorithms.HmacSha256);
        Assert.IsNull(selector.Select(_httpContext));
        _httpContext.Request.Headers.Authorization = "Bearer " + OidcTestHost.Token();
        TokenValidatedFor(new Customer { Id = "token", Active = true });

        var customer = await new OidcRequestCustomerResolver(_accessor.Object, selector).Resolve();

        Assert.IsNull(customer);
        _aspNetAuthentication.Verify(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ValidToken_TokenCustomerWinsOverCookieCustomer()
    {
        TokenValidatedFor(new Customer { Id = "token", Active = true });

        var customer = await CreateSetter().CurrentCustomerForTest(new Store());

        Assert.AreEqual("token", customer.Id);
        _authentication.Verify(a => a.GetAuthenticatedCustomer(), Times.Never);
    }

    [TestMethod]
    public async Task ValidToken_TokenCustomerWinsOnAllowAnonymousEndpoint()
    {
        _httpContext.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(new AllowAnonymousAttribute()), "test"));
        TokenValidatedFor(new Customer { Id = "token", Active = true });

        var customer = await CreateSetter().CurrentCustomerForTest(new Store());

        Assert.AreEqual("token", customer.Id);
    }

    [TestMethod]
    public async Task ValidToken_TokenCustomerNeverImpersonated()
    {
        var tokenCustomer = new Customer { Id = "token", Active = true };
        tokenCustomer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.ImpersonatedCustomerId, Value = "victim", StoreId = "" });
        _customerService.Setup(c => c.GetCustomerById("victim")).ReturnsAsync(new Customer { Id = "victim", Active = true });
        TokenValidatedFor(tokenCustomer);
        var setter = CreateSetter();

        var customer = await setter.CurrentCustomerForTest(new Store());

        Assert.AreEqual("token", customer.Id);
        Assert.IsNull(setter.OriginalCustomerForTest);
    }

    [TestMethod]
    public async Task RefusedToken_BuiltInStepsDecide()
    {
        TokenRefused();

        var customer = await CreateSetter().CurrentCustomerForTest(new Store());

        Assert.AreEqual("cookie", customer.Id);
    }

    [TestMethod]
    public async Task RefusedToken_NoOtherCredential_AnonymousNotNewGuest()
    {
        TokenRefused();
        _authentication.Setup(a => a.GetAuthenticatedCustomer()).ReturnsAsync((Customer)null);

        var customer = await CreateSetter().CurrentCustomerForTest(new Store());

        Assert.AreEqual("anonymous", customer.Id);
        _customerService.Verify(c => c.InsertGuestCustomer(It.IsAny<Customer>()), Times.Never);
    }

    [TestMethod]
    public async Task PathOutsideProtectedResources_TokenNotAuthenticated()
    {
        _httpContext.Request.Path = "/checkout";
        TokenValidatedFor(new Customer { Id = "token", Active = true });

        var customer = await CreateSetter().CurrentCustomerForTest(new Store());

        Assert.AreEqual("cookie", customer.Id);
        _aspNetAuthentication.Verify(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task TokenOfAnotherIssuer_LeftToTheWebApiScheme()
    {
        //the SecretKey JWT GrandNode issues itself: HMAC-signed, another issuer
        var secretKey = new SymmetricSecurityKey(new byte[32]);
        _httpContext.Request.Headers.Authorization = "Bearer " + OidcTestHost.Token(key: secretKey,
            algorithm: SecurityAlgorithms.HmacSha256, issuer: "https://grandnode.api");
        _authentication.Setup(a => a.GetAuthenticatedCustomer()).ReturnsAsync((Customer)null);
        _apiAuthentication.Setup(a => a.GetAuthenticatedCustomer()).ReturnsAsync(new Customer { Id = "api", Active = true });
        TokenValidatedFor(new Customer { Id = "token", Active = true });

        var customer = await CreateSetter().CurrentCustomerForTest(new Store());

        Assert.AreEqual("api", customer.Id);
        _aspNetAuthentication.Verify(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()), Times.Never);
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
                .GetField("_originalCustomerIfImpersonated",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(this);

        public Task<Customer> CurrentCustomerForTest(Store store) => CurrentCustomer(store);
    }
}
