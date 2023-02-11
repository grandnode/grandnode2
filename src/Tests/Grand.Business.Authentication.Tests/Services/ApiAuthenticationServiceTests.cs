using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Authentication.Services;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Claims;

namespace Grand.Business.Authentication.Tests.Services
{
    [TestClass()]
    public class ApiAuthenticationServiceTests
    {
        private Mock<IHttpContextAccessor> _httpContextAccessorMoc;
        private Mock<ICustomerService> _customerService;
        private Mock<IUserApiService> _userApiServiceMock;
        private Mock<HttpContext> _httpContextMock;
        private Mock<IGroupService> _groupService;
        private IApiAuthenticationService _authService;
        private IJwtBearerAuthenticationService _jwtBearerAuthenticationService;

        [TestInitialize()]
        public void Init()
        {
            _httpContextAccessorMoc = new Mock<IHttpContextAccessor>();
            _customerService = new Mock<ICustomerService>();
            _userApiServiceMock = new Mock<IUserApiService>();
            _groupService = new Mock<IGroupService>();
            _httpContextMock = new Mock<HttpContext>();
            _authService = new ApiAuthenticationService(_httpContextAccessorMoc.Object, _customerService.Object, _groupService.Object);
            _jwtBearerAuthenticationService = new JwtBearerAuthenticationService(_customerService.Object, _userApiServiceMock.Object);
        }

        [TestMethod()]
        public async Task Valid_NullEmail_ReturnFalse()
        {
            var httpContext = new Mock<HttpContext>();
            var context = new TokenValidatedContext(httpContext.Object, new AuthenticationScheme("", "", typeof(AuthSchemaMock)), new JwtBearerOptions());
            IList<Claim> claims = new List<Claim>
            {
                 new Claim("Token", "123")
            };
            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ""));
            var result = await _jwtBearerAuthenticationService.Valid(context);
            Assert.IsFalse(result);
            Assert.AreEqual(await _jwtBearerAuthenticationService.ErrorMessage(), "Email not exists in the context");
        }

        [TestMethod()]
        public async Task Valid_NullToken_ReturnFalse()
        {
            var httpContext = new Mock<HttpContext>();
            var context = new TokenValidatedContext(httpContext.Object, new AuthenticationScheme("", "", typeof(AuthSchemaMock)), new JwtBearerOptions());
            IList<Claim> claims = new List<Claim>
            {
                  new Claim("Email", "johny@gmail.com"),
            };
            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ""));
            var result = await _jwtBearerAuthenticationService.Valid(context);
            Assert.IsFalse(result);
            Assert.AreEqual(await _jwtBearerAuthenticationService.ErrorMessage(), "Wrong token, change password on the customer and create token again");
        }

        [TestMethod()]
        public async Task Valid_NotFoundCoustomer_ReturnFalse()
        {
            var httpContext = new Mock<HttpContext>();
            var context = new TokenValidatedContext(httpContext.Object, new AuthenticationScheme("", "", typeof(AuthSchemaMock)), new JwtBearerOptions());
            IList<Claim> claims = new List<Claim>
            {
                 new Claim("Email", "johny@gmail.com"),
                 new Claim("Token", "123")
            };
            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ""));
            _customerService.Setup(c => c.GetCustomerByEmail(It.IsAny<string>())).Returns(() => Task.FromResult<Customer>(null));
            var result = await _jwtBearerAuthenticationService.Valid(context);
            Assert.IsFalse(result);
            Assert.AreEqual(await _jwtBearerAuthenticationService.ErrorMessage(), "Email not exists/or not active in the customer table");
        }

        [TestMethod()]
        public async Task Valid_NotActiveCustomer_ReturnFalse()
        {
            var httpContext = new Mock<HttpContext>();
            var context = new TokenValidatedContext(httpContext.Object, new AuthenticationScheme("", "", typeof(AuthSchemaMock)), new JwtBearerOptions());
            IList<Claim> claims = new List<Claim>
            {
                 new Claim("Email", "johny@gmail.com"),
                 new Claim("Token", "123")
            };
            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ""));
            _customerService.Setup(c => c.GetCustomerByEmail(It.IsAny<string>())).Returns(() => Task.FromResult(new Customer() { Active = false }));
            var result = await _jwtBearerAuthenticationService.Valid(context);
            Assert.IsFalse(result);
            Assert.AreEqual(await _jwtBearerAuthenticationService.ErrorMessage(), "Email not exists/or not active in the customer table");
        }

        [TestMethod()]
        public async Task Valid_NotActiveUserApi_ReturnFalse()
        {
            var httpContext = new Mock<HttpContext>();
            var context = new TokenValidatedContext(httpContext.Object, new AuthenticationScheme("", "", typeof(AuthSchemaMock)), new JwtBearerOptions());
            IList<Claim> claims = new List<Claim>
            {
                 new Claim("Email", "johny@gmail.com"),
                 new Claim("Token", "123")
            };
            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ""));
            _customerService.Setup(c => c.GetCustomerByEmail(It.IsAny<string>())).Returns(() => Task.FromResult(new Customer() { Active = true }));
            _userApiServiceMock.Setup(c => c.GetUserByEmail(It.IsAny<string>())).Returns(() => Task.FromResult(new UserApi() { IsActive = false, Token = "123" }));
            var result = await _jwtBearerAuthenticationService.Valid(context);
            Assert.IsFalse(result);
            Assert.AreEqual("User api not exists/or not active in the user api table", await _jwtBearerAuthenticationService.ErrorMessage());
        }
        [TestMethod()]
        public async Task Valid_WrongTokenUserApi_ReturnFalse()
        {
            var httpContext = new Mock<HttpContext>();
            var context = new TokenValidatedContext(httpContext.Object, new AuthenticationScheme("", "", typeof(AuthSchemaMock)), new JwtBearerOptions());
            IList<Claim> claims = new List<Claim>
            {
                 new Claim("Email", "johny@gmail.com"),
                 new Claim("Token", "123")
            };
            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ""));
            _customerService.Setup(c => c.GetCustomerByEmail(It.IsAny<string>())).Returns(() => Task.FromResult(new Customer() { Active = true }));
            _userApiServiceMock.Setup(c => c.GetUserByEmail(It.IsAny<string>())).Returns(() => Task.FromResult(new UserApi() { IsActive = true, Token = "321" }));
            var result = await _jwtBearerAuthenticationService.Valid(context);
            Assert.IsFalse(result);
            Assert.AreEqual("Wrong token, generate again", await _jwtBearerAuthenticationService.ErrorMessage());
        }
        [TestMethod()]
        public async Task Valid_ActiveUserApi_ReturnTrue()
        {
            var httpContext = new Mock<HttpContext>();
            var context = new TokenValidatedContext(httpContext.Object, new AuthenticationScheme("", "", typeof(AuthSchemaMock)), new JwtBearerOptions());
            IList<Claim> claims = new List<Claim>
            {
                 new Claim("Email", "johny@gmail.com"),
                 new Claim("Token", "123")
            };
            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ""));
            _customerService.Setup(c => c.GetCustomerByEmail(It.IsAny<string>())).Returns(() => Task.FromResult(new Customer() { Active = true }));
            _userApiServiceMock.Setup(c => c.GetUserByEmail(It.IsAny<string>())).Returns(() => Task.FromResult(new UserApi() { IsActive = true, Token = "123" }));
            var result = await _jwtBearerAuthenticationService.Valid(context);
            Assert.IsTrue(result);
        }



        [TestMethod()]
        public async Task GetAuthenticatedCustomer_NullAuthHeader_GetNull()
        {
            var customer = new Customer() { Email = "johny@gmail.com" };
            var httpContext = new Mock<HttpContext>();
            var req = new Mock<HttpRequest>();
            req.Setup(c => c.Path).Returns(new PathString("/api/..."));
            req.Setup(c => c.Headers).Returns(new HeaderDictionary());
            httpContext.Setup(c => c.Request).Returns(req.Object);
            _httpContextAccessorMoc.Setup(c => c.HttpContext).Returns(httpContext.Object);
            _customerService.Setup(c => c.GetCustomerBySystemName(It.IsAny<string>())).Returns(() => Task.FromResult(customer));
            var result = await _authService.GetAuthenticatedCustomer();
            Assert.IsNull(result);
        }

    }
}