using Grand.Business.Authentication.Services;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Claims;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class JwtBearerCustomerAuthenticationServiceTests
{
    private Mock<ICustomerService> _customerServiceMock;
    private Mock<IGroupService> _groupService;
    private Mock<HttpContext> _httpContextMock;

    private IJwtBearerCustomerAuthenticationService _jwtBearerCustomerAuthenticationService;
    private Mock<IPermissionService> _permissionServiceMock;
    private Mock<IRefreshTokenService> _refreshTokenServiceMock;

    [TestInitialize]
    public void Init()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _groupService = new Mock<IGroupService>();
        _permissionServiceMock = new Mock<IPermissionService>();
        _httpContextMock = new Mock<HttpContext>();
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _jwtBearerCustomerAuthenticationService = new JwtBearerCustomerAuthenticationService(
            _customerServiceMock.Object, _permissionServiceMock.Object, _groupService.Object,
            _refreshTokenServiceMock.Object);
    }

    [TestMethod]
    public async Task Valid_NullEmail_ReturnFalse()
    {
        var httpContext = new Mock<HttpContext>();
        var context = new TokenValidatedContext(httpContext.Object,
            new AuthenticationScheme("", "", typeof(AuthSchemaMock)), new JwtBearerOptions());
        IList<Claim> claims = new List<Claim> {
            new("Token", "123"),
            new("Email", "admin@yourstore.com"),
            new("RefreshId", "567")
        };
        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ""));
        var result = await _jwtBearerCustomerAuthenticationService.Valid(context);
        Assert.IsFalse(result);
        Assert.AreEqual(await _jwtBearerCustomerAuthenticationService.ErrorMessage(), "Not found customer");
    }

    [TestMethod]
    public async Task Valid_NullToken_ReturnFalse()
    {
        var expectedCustomer = new Customer { Username = "John", Active = true };
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>()))
            .Returns(() => Task.FromResult(expectedCustomer));

        var httpContext = new Mock<HttpContext>();
        var context = new TokenValidatedContext(httpContext.Object,
            new AuthenticationScheme("", "", typeof(AuthSchemaMock)), new JwtBearerOptions());
        IList<Claim> claims = new List<Claim> {
            new("Email", "admin@yourstore.com")
        };
        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ""));
        var result = await _jwtBearerCustomerAuthenticationService.Valid(context);
        Assert.IsFalse(result);
        Assert.AreEqual(await _jwtBearerCustomerAuthenticationService.ErrorMessage(),
            "Invalid token or cancel by refresh token");
    }


    [TestMethod]
    public async Task Valid_NotActiveCustomer_ReturnFalse()
    {
        var expectedCustomer = new Customer { Username = "John", Active = true };
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>()))
            .Returns(() => Task.FromResult(expectedCustomer));

        var httpContext = new Mock<HttpContext>();
        var context = new TokenValidatedContext(httpContext.Object,
            new AuthenticationScheme("", "", typeof(AuthSchemaMock)), new JwtBearerOptions());
        IList<Claim> claims = new List<Claim> {
            new("Email", "johny@gmail.com"),
            new("Token", "123")
        };
        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ""));
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>()))
            .Returns(() => Task.FromResult(new Customer { Active = false }));
        var result = await _jwtBearerCustomerAuthenticationService.Valid(context);
        Assert.IsFalse(result);
        Assert.AreEqual(await _jwtBearerCustomerAuthenticationService.ErrorMessage(),
            "Customer not exists/or not active in the customer table");
    }

    [TestMethod]
    public async Task Valid_NoPermissions_Customer_ReturnFalse()
    {
        var customer = new Customer { Username = "John", Active = true };
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>()))
            .Returns(() => Task.FromResult(customer));
        _refreshTokenServiceMock.Setup(c => c.GetCustomerRefreshToken(customer)).Returns(() =>
            Task.FromResult(new RefreshToken { IsActive = true, RefreshId = "567", Token = "123" }));
        _permissionServiceMock.Setup(c => c.Authorize(StandardPermission.AllowUseApi, customer))
            .Returns(() => Task.FromResult(false));
        var httpContext = new Mock<HttpContext>();
        var context = new TokenValidatedContext(httpContext.Object,
            new AuthenticationScheme("", "", typeof(AuthSchemaMock)), new JwtBearerOptions());
        IList<Claim> claims = new List<Claim> {
            new("Email", "johny@gmail.com"),
            new("Token", "123"),
            new("RefreshId", "567")
        };
        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ""));

        var result = await _jwtBearerCustomerAuthenticationService.Valid(context);
        Assert.IsFalse(result);
        Assert.AreEqual(await _jwtBearerCustomerAuthenticationService.ErrorMessage(),
            "You do not have permission to use API operation (Customer group)");
    }

    [TestMethod]
    public async Task Valid_Customer_ReturnTrue()
    {
        var customer = new Customer { Username = "John", Active = true };
        customer.UserFields.Add(new UserField
            { Key = SystemCustomerFieldNames.PasswordToken, Value = "123", StoreId = "" });
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>()))
            .Returns(() => Task.FromResult(customer));
        _refreshTokenServiceMock.Setup(c => c.GetCustomerRefreshToken(customer)).Returns(() =>
            Task.FromResult(new RefreshToken { IsActive = true, RefreshId = "567", Token = "123" }));
        _permissionServiceMock.Setup(c => c.Authorize(StandardPermission.AllowUseApi, customer))
            .Returns(() => Task.FromResult(true));
        var httpContext = new Mock<HttpContext>();
        var context = new TokenValidatedContext(httpContext.Object,
            new AuthenticationScheme("", "", typeof(AuthSchemaMock)), new JwtBearerOptions());
        IList<Claim> claims = new List<Claim> {
            new("Email", "johny@gmail.com"),
            new("Token", "123"),
            new("RefreshId", "567")
        };
        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ""));

        var result = await _jwtBearerCustomerAuthenticationService.Valid(context);
        Assert.IsTrue(result);
    }
}