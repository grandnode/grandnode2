using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.SharedKernel.Attributes;
using Grand.SharedKernel.Extensions;
using Grand.Web.Common.Security.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Claims;

namespace Grand.Web.Common.Tests.Security.Authentication;

[TestClass]
public class ApiAuthenticationServiceTests
{
    private IApiAuthenticationService _authService;
    private Mock<ICustomerService> _customerService;
    private Mock<IGroupService> _groupService;
    private Mock<IHttpContextAccessor> _httpContextAccessorMoc;

    [TestInitialize]
    public void Init()
    {
        _httpContextAccessorMoc = new Mock<IHttpContextAccessor>();
        _customerService = new Mock<ICustomerService>();
        _groupService = new Mock<IGroupService>();
        _authService = new ApiAuthenticationService(_customerService.Object, _groupService.Object,
            _httpContextAccessorMoc.Object, new Mock<IAuthenticationSchemeProvider>().Object);
    }

    [TestMethod]
    public async Task GetAuthenticatedCustomer_NullAuthHeader_GetNull()
    {
        var customer = new Customer { Email = "johny@gmail.com" };
        var httpContext = new Mock<HttpContext>();
        var req = new Mock<HttpRequest>();
        req.Setup(c => c.Path).Returns(new PathString("/api/..."));
        req.Setup(c => c.Headers).Returns(new HeaderDictionary());
        httpContext.Setup(c => c.Request).Returns(req.Object);
        _httpContextAccessorMoc.Setup(c => c.HttpContext).Returns(httpContext.Object);
        _customerService.Setup(c => c.GetCustomerBySystemName(It.IsAny<string>()))
            .Returns(() => Task.FromResult(customer));
        var result = await _authService.GetAuthenticatedCustomer();
        Assert.IsNull(result);
    }

    private static HttpContext CreateHttpContext(Action<AuthenticationBuilder> configure, Endpoint endpoint = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddAuthentication();
        configure?.Invoke(builder);
        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        context.Request.Headers["Authorization"] = "Bearer anything";
        if (endpoint != null) context.SetEndpoint(endpoint);
        return context;
    }

    private IApiAuthenticationService CreateService(HttpContext context)
    {
        _httpContextAccessorMoc.Setup(c => c.HttpContext).Returns(context);
        return new ApiAuthenticationService(_customerService.Object, _groupService.Object,
            _httpContextAccessorMoc.Object,
            context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>());
    }

    [TestMethod]
    public async Task GetAuthenticatedCustomer_BearerHeaderWithoutJwtScheme_ReturnsNull()
    {
        var context = CreateHttpContext(null);
        var result = await CreateService(context).GetAuthenticatedCustomer();
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetAuthenticatedCustomer_FrontApiEndpointWithoutFrontScheme_ReturnsNull()
    {
        var endpoint = new Endpoint(null,
            new EndpointMetadataCollection(new ApiGroupAttribute(ApiConstants.ApiGroupNameV2)), "front");
        var context = CreateHttpContext(null, endpoint);
        var result = await CreateService(context).GetAuthenticatedCustomer();
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetAuthenticatedCustomer_JwtSchemeRegisteredAndSucceeds_ReturnsCustomer()
    {
        var customer = new Customer { Id = "c1", Active = true };
        _customerService.Setup(c => c.GetCustomerById("c1")).ReturnsAsync(customer);
        _groupService.Setup(g => g.IsRegistered(customer)).ReturnsAsync(true);
        var context = CreateHttpContext(b => b.AddScheme<AuthenticationSchemeOptions, OkHandler>(
            JwtBearerDefaults.AuthenticationScheme, null));
        var result = await CreateService(context).GetAuthenticatedCustomer();
        Assert.AreSame(customer, result);
    }

    [TestMethod]
    public async Task GetAuthenticatedCustomer_BearerForwardedToOidc_ReturnsNull()
    {
        //a provider token the Bearer scheme forwards to the Oidc scheme: its CustomerId claim was not issued by GrandNode
        var customer = new Customer { Id = "c1", Active = true };
        _customerService.Setup(c => c.GetCustomerById("c1")).ReturnsAsync(customer);
        _groupService.Setup(g => g.IsRegistered(customer)).ReturnsAsync(true);
        var context = CreateHttpContext(b => b
            .AddScheme<AuthenticationSchemeOptions, OkHandler>(JwtBearerDefaults.AuthenticationScheme,
                o => o.ForwardDefault = "Oidc")
            .AddScheme<AuthenticationSchemeOptions, OkHandler>("Oidc", null));

        var result = await CreateService(context).GetAuthenticatedCustomer();

        Assert.IsNull(result);
        _customerService.Verify(c => c.GetCustomerById(It.IsAny<string>()), Times.Never);
        _customerService.Verify(c => c.GetCustomerByEmail(It.IsAny<string>()), Times.Never);
    }

    private class OkHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public OkHandler(Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
            Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity(new[] { new Claim("CustomerId", "c1") }, "test");
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}