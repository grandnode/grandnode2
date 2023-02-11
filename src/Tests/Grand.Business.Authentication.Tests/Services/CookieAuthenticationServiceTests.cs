﻿using Grand.Business.Authentication.Services;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Claims;

namespace Grand.Business.Authentication.Tests.Services
{
    [TestClass()]
    public class CookieAuthenticationServiceTests
    {
        private Mock<ICustomerService> _customerServiceMock;
        private Mock<IHttpContextAccessor> _httpAccessorMock;
        private CustomerSettings _customerSettings;
        private CookieAuthenticationService _cookieAuthService;
        private Mock<IAuthenticationService> _authServiceMock;
        private Mock<IServiceProvider> serviceProviderMock;
        private Mock<IGroupService> _groupServiceMock;
        private Mock<IUserFieldService> _userFieldServiceMock;
        private DefaultHttpContext _httpContext;
        private SecurityConfig _config;

        [TestInitialize()]
        public void Init()
        {
            _customerServiceMock = new Mock<ICustomerService>();

            _httpAccessorMock = new Mock<IHttpContextAccessor>();
            _customerSettings = new CustomerSettings();
            _groupServiceMock = new Mock<IGroupService>();
            _userFieldServiceMock = new Mock<IUserFieldService>();
            _config = new SecurityConfig();
            _config.CookieClaimsIssuer = "grandnode";
            _config.CookiePrefix = ".Grand.";
            _cookieAuthService = new CookieAuthenticationService(_customerSettings, _customerServiceMock.Object, _groupServiceMock.Object, _userFieldServiceMock.Object, _httpAccessorMock.Object, _config);
            //For mock HttpContext extension methods like SignOutAsync ,SignInAsync etc..
            _authServiceMock = new Mock<IAuthenticationService>();
            serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(_ => _.GetService(typeof(IAuthenticationService)))
                .Returns(_authServiceMock.Object);
            _httpContext = new DefaultHttpContext() { RequestServices = serviceProviderMock.Object };
            _httpAccessorMock.Setup(c => c.HttpContext).Returns(_httpContext);
        }

        [TestMethod()]
        public async Task SignOut()
        {
            _authServiceMock.Setup(c => c.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()));
            await _cookieAuthService.SignOut();
            _authServiceMock.Verify(c => c.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()), Times.AtLeastOnce);
        }



        [TestMethod()]
        public async Task SignIn_NullCustomer_ThrowException()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _cookieAuthService.SignIn(null, false));
        }


        [TestMethod()]
        public async Task SignIn_ValidCustomer()
        {
            _authServiceMock.Setup(c => c.SignInAsync(It.IsAny<HttpContext>(),
                It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>())).Returns(() => Task.CompletedTask);
            await _cookieAuthService.SignIn(new Customer(), false);
            _authServiceMock.Verify(c => c.SignInAsync(It.IsAny<HttpContext>(),
                It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Once);
        }

        [TestMethod()]
        public async Task GetAuthenticatedCustomer_AuthenticatedFaild_ReturnNull()
        {
            _authServiceMock.Setup(c => c.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
                .Returns(() => Task.FromResult(AuthenticateResult.Fail(new Exception())));
            var customer = await _cookieAuthService.GetAuthenticatedCustomer();
            Assert.IsNull(customer);
        }

        [TestMethod()]
        public async Task GetAuthenticatedCustomer_UsernameEnableRegisterd_ReturnCustomer()
        {
            var expectedCustomer = new Customer() { Username = "John", Active = true };

            _customerSettings.UsernamesEnabled = true;
            var cliaim = new Claim(ClaimTypes.Name, "Johny", "", "grandnode");
            IList<Claim> claims = new List<Claim>
            {
                 new Claim(ClaimTypes.Name,ClaimTypes.Name,"", "grandnode")
              };
            var principals = new ClaimsPrincipal(new ClaimsIdentity(claims, GrandCookieAuthenticationDefaults.AuthenticationScheme));
            _authServiceMock.Setup(c => c.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
                .Returns(() => Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principals, ""))));
            _customerServiceMock.Setup(c => c.GetCustomerByUsername(It.IsAny<string>())).Returns(() => Task.FromResult(expectedCustomer));
            _groupServiceMock.Setup(c => c.IsRegistered(It.IsAny<Customer>())).Returns(() => Task.FromResult(true));

            var customer = await _cookieAuthService.GetAuthenticatedCustomer();
            Assert.AreEqual(customer.Username, expectedCustomer.Username);
        }

        [TestMethod()]
        public async Task GetAuthenticatedCustomer_UsernameEnableGuests_ReturnNull()
        {
            var expectedCustomer = new Customer() { Username = "John", Active = true };
            _customerSettings.UsernamesEnabled = true;
            var cliaim = new Claim(ClaimTypes.Name, "Johny", "", "grandnode");
            IList<Claim> claims = new List<Claim>
            {
                 new Claim(ClaimTypes.Name,ClaimTypes.Name,"", "grandnode")
              };
            var principals = new ClaimsPrincipal(new ClaimsIdentity(claims, GrandCookieAuthenticationDefaults.AuthenticationScheme));
            _authServiceMock.Setup(c => c.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
                .Returns(() => Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principals, ""))));
            _customerServiceMock.Setup(c => c.GetCustomerByUsername(It.IsAny<string>())).Returns(() => Task.FromResult(expectedCustomer));
            //guest
            _groupServiceMock.Setup(c => c.IsRegistered(It.IsAny<Customer>())).Returns(() => Task.FromResult(false));
            var customer = await _cookieAuthService.GetAuthenticatedCustomer();
            Assert.IsNull(customer);
            //_customerServiceMock.Verify(c => c.GetCustomerByUsername(It.IsAny<string>()), Times.Once);
        }

    }
}
