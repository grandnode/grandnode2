using Grand.Business.Authentication.Services;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Claims;

#pragma warning disable CS0618 //API users stay supported until removed

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class JwtBearerAuthenticationServiceTests
{
    private Mock<ICustomerService> _customerService;
    private IJwtBearerAuthenticationService _jwtBearerAuthenticationService;
    private Mock<IUserApiService> _userApiServiceMock;
    private OidcLoginConfig _oidcLoginConfig;

    [TestInitialize]
    public void Init()
    {
        _customerService = new Mock<ICustomerService>();
        _userApiServiceMock = new Mock<IUserApiService>();
        _oidcLoginConfig = new OidcLoginConfig();
        _jwtBearerAuthenticationService =
            new JwtBearerAuthenticationService(_customerService.Object, _userApiServiceMock.Object, _oidcLoginConfig);
    }

    [TestMethod]
    public async Task Valid_NullEmail_ReturnFalse()
    {
        var httpContext = new Mock<HttpContext>();
        var context = new TokenValidatedContext(httpContext.Object,
            new AuthenticationScheme("", "", typeof(AuthSchemaMock)), new JwtBearerOptions());
        IList<Claim> claims = new List<Claim> {
            new("Token", "123")
        };
        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ""));
        var result = await _jwtBearerAuthenticationService.Valid(context);
        Assert.IsFalse(result);
        Assert.AreEqual("Email not exists in the context", await _jwtBearerAuthenticationService.ErrorMessage());
    }

    [TestMethod]
    public async Task Valid_NullToken_ReturnFalse()
    {
        var httpContext = new Mock<HttpContext>();
        var context = new TokenValidatedContext(httpContext.Object,
            new AuthenticationScheme("", "", typeof(AuthSchemaMock)), new JwtBearerOptions());
        IList<Claim> claims = new List<Claim> {
            new("Email", "johny@gmail.com")
        };
        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ""));
        var result = await _jwtBearerAuthenticationService.Valid(context);
        Assert.IsFalse(result);
        Assert.AreEqual("Wrong token, change password on the customer and create token again",
            await _jwtBearerAuthenticationService.ErrorMessage());
    }

    [TestMethod]
    public async Task Valid_NotFoundCoustomer_ReturnFalse()
    {
        var httpContext = new Mock<HttpContext>();
        var context = new TokenValidatedContext(httpContext.Object,
            new AuthenticationScheme("", "", typeof(AuthSchemaMock)), new JwtBearerOptions());
        IList<Claim> claims = new List<Claim> {
            new("Email", "johny@gmail.com"),
            new("Token", "123")
        };
        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ""));
        _customerService.Setup(c => c.GetCustomerByEmail(It.IsAny<string>()))
            .Returns(() => Task.FromResult<Customer>(null));
        var result = await _jwtBearerAuthenticationService.Valid(context);
        Assert.IsFalse(result);
        Assert.AreEqual("Email not exists/or not active in the customer table",
            await _jwtBearerAuthenticationService.ErrorMessage());
    }

    [TestMethod]
    public async Task Valid_NotActiveCustomer_ReturnFalse()
    {
        var httpContext = new Mock<HttpContext>();
        var context = new TokenValidatedContext(httpContext.Object,
            new AuthenticationScheme("", "", typeof(AuthSchemaMock)), new JwtBearerOptions());
        IList<Claim> claims = new List<Claim> {
            new("Email", "johny@gmail.com"),
            new("Token", "123")
        };
        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ""));
        _customerService.Setup(c => c.GetCustomerByEmail(It.IsAny<string>()))
            .Returns(() => Task.FromResult(new Customer { Active = false }));
        var result = await _jwtBearerAuthenticationService.Valid(context);
        Assert.IsFalse(result);
        Assert.AreEqual("Email not exists/or not active in the customer table",
            await _jwtBearerAuthenticationService.ErrorMessage());
    }

    [TestMethod]
    public async Task Valid_NotActiveUserApi_ReturnFalse()
    {
        var httpContext = new Mock<HttpContext>();
        var context = new TokenValidatedContext(httpContext.Object,
            new AuthenticationScheme("", "", typeof(AuthSchemaMock)), new JwtBearerOptions());
        IList<Claim> claims = new List<Claim> {
            new("Email", "johny@gmail.com"),
            new("Token", "123")
        };
        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ""));
        _customerService.Setup(c => c.GetCustomerByEmail(It.IsAny<string>()))
            .Returns(() => Task.FromResult(new Customer { Active = true }));
        _userApiServiceMock.Setup(c => c.GetUserByEmail(It.IsAny<string>()))
            .Returns(() => Task.FromResult(new UserApi { IsActive = false, Token = "123" }));
        var result = await _jwtBearerAuthenticationService.Valid(context);
        Assert.IsFalse(result);
        Assert.AreEqual("User api not exists/or not active in the user api table",
            await _jwtBearerAuthenticationService.ErrorMessage());
    }

    [TestMethod]
    public async Task Valid_WrongTokenUserApi_ReturnFalse()
    {
        var httpContext = new Mock<HttpContext>();
        var context = new TokenValidatedContext(httpContext.Object,
            new AuthenticationScheme("", "", typeof(AuthSchemaMock)), new JwtBearerOptions());
        IList<Claim> claims = new List<Claim> {
            new("Email", "johny@gmail.com"),
            new("Token", "123")
        };
        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ""));
        _customerService.Setup(c => c.GetCustomerByEmail(It.IsAny<string>()))
            .Returns(() => Task.FromResult(new Customer { Active = true }));
        _userApiServiceMock.Setup(c => c.GetUserByEmail(It.IsAny<string>()))
            .Returns(() => Task.FromResult(new UserApi { IsActive = true, Token = "321" }));
        var result = await _jwtBearerAuthenticationService.Valid(context);
        Assert.IsFalse(result);
        Assert.AreEqual("Wrong token, generate again", await _jwtBearerAuthenticationService.ErrorMessage());
    }

    [TestMethod]
    public async Task Valid_ActiveUserApi_ReturnTrue()
    {
        var httpContext = new Mock<HttpContext>();
        var context = new TokenValidatedContext(httpContext.Object,
            new AuthenticationScheme("", "", typeof(AuthSchemaMock)), new JwtBearerOptions());
        IList<Claim> claims = new List<Claim> {
            new("Email", "johny@gmail.com"),
            new("Token", "123")
        };
        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ""));
        _customerService.Setup(c => c.GetCustomerByEmail(It.IsAny<string>()))
            .Returns(() => Task.FromResult(new Customer { Active = true }));
        _userApiServiceMock.Setup(c => c.GetUserByEmail(It.IsAny<string>()))
            .Returns(() => Task.FromResult(new UserApi { IsActive = true, Token = "123" }));
        var result = await _jwtBearerAuthenticationService.Valid(context);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task Valid_ProviderOnly_ActiveUserApi_ReturnFalse()
    {
        _oidcLoginConfig.SignInMode = OidcSignInMode.ProviderOnly;
        var context = new TokenValidatedContext(new Mock<HttpContext>().Object,
            new AuthenticationScheme("", "", typeof(AuthSchemaMock)), new JwtBearerOptions());
        IList<Claim> claims = new List<Claim> {
            new("Email", "johny@gmail.com"),
            new("Token", "123")
        };
        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ""));
        _customerService.Setup(c => c.GetCustomerByEmail(It.IsAny<string>()))
            .Returns(() => Task.FromResult(new Customer { Active = true }));
        _userApiServiceMock.Setup(c => c.GetUserByEmail(It.IsAny<string>()))
            .Returns(() => Task.FromResult(new UserApi { IsActive = true, Token = "123" }));

        var result = await _jwtBearerAuthenticationService.Valid(context);

        Assert.IsFalse(result);
        Assert.AreEqual("Sign-in with API users is disabled: use access tokens from the identity provider",
            await _jwtBearerAuthenticationService.ErrorMessage());
    }
}
