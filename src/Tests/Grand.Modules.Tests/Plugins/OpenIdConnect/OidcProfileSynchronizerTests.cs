using Authentication.OpenIdConnect.Infrastructure;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.ExternalRegistration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq.Expressions;

namespace Grand.Modules.Tests.Plugins.OpenIdConnect;

[TestClass]
public class OidcProfileSynchronizerTests
{
    private Mock<ICustomerService> _customers;
    private Mock<ILogger<OidcProfileSynchronizer>> _logger;
    private CustomerSettings _settings;
    private CustomerConfig _config;
    private OidcLoginConfig _login;
    private Customer _customer;

    [TestInitialize]
    public void Init()
    {
        _customers = new Mock<ICustomerService>();
        _logger = new Mock<ILogger<OidcProfileSynchronizer>>();
        _settings = new CustomerSettings { UsernamesEnabled = false };
        _config = new CustomerConfig();
        _login = new OidcLoginConfig { SignInMode = OidcSignInMode.ProviderOnly };
        _customer = new Customer { Id = "c1", Email = "old@shop.com", Username = "old@shop.com" };
        _customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.FirstName, Value = "John", StoreId = "" });
        _customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.LastName, Value = "Doe", StoreId = "" });
    }

    private Task Sync(string email = "old@shop.com", bool verified = true, string first = "John", string last = "Doe") =>
        new OidcProfileSynchronizer(_customers.Object, _settings, _config, _login, _logger.Object)
            .Sync(_customer, new ExternalRegistrationIdentity("OpenIdConnect", "sub-1", email, verified, first, last), "s1");

    private void VerifyEmail(string value, Times times) =>
        _customers.Verify(x => x.UpdateCustomerField(_customer, It.IsAny<Expression<Func<Customer, string>>>(), value), times);

    [TestMethod]
    public async Task Mixed_NothingWritten()
    {
        _login.SignInMode = OidcSignInMode.Mixed;
        await Sync("new@shop.com", first: "Jane", last: "Roe");
        _customers.Verify(x => x.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        VerifyEmail("new@shop.com", Times.Never());
    }

    [TestMethod]
    public async Task Names_Written_WhenChanged()
    {
        await Sync(first: "Jane", last: "Roe");
        _customers.Verify(x => x.UpdateUserField(_customer, SystemCustomerFieldNames.FirstName, "Jane", ""), Times.Once);
        _customers.Verify(x => x.UpdateUserField(_customer, SystemCustomerFieldNames.LastName, "Roe", ""), Times.Once);
    }

    [TestMethod]
    public async Task Names_Empty_Kept()
    {
        await Sync(first: "", last: null);
        _customers.Verify(x => x.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Names_Same_NotWritten()
    {
        await Sync();
        _customers.Verify(x => x.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Email_BreakGlassAccount_Kept_NamesStillSynced()
    {
        _login.BreakGlassAccounts = ["Old@Shop.com"];

        await Sync("new@shop.com", first: "Jane");

        VerifyEmail("new@shop.com", Times.Never());
        Assert.AreEqual("old@shop.com", _customer.Email);
        _customers.Verify(x => x.UpdateUserField(_customer, SystemCustomerFieldNames.FirstName, "Jane", ""), Times.Once);
        _logger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    [TestMethod]
    public async Task Email_Verified_Changed_WrittenWithUsername()
    {
        await Sync("New@Shop.com");
        VerifyEmail("new@shop.com", Times.Exactly(2));
        Assert.AreEqual("new@shop.com", _customer.Email);
        Assert.AreEqual("new@shop.com", _customer.Username);
    }

    [TestMethod]
    public async Task Email_UsernamesEnabled_UsernameKept()
    {
        _settings.UsernamesEnabled = true;
        _customer.Username = "johnny";
        await Sync("new@shop.com");
        VerifyEmail("new@shop.com", Times.Once());
        Assert.AreEqual("johnny", _customer.Username);
    }

    [TestMethod]
    public async Task Email_NotVerified_Kept()
    {
        await Sync("new@shop.com", false);
        VerifyEmail("new@shop.com", Times.Never());
        Assert.AreEqual("old@shop.com", _customer.Email);
    }

    [TestMethod]
    public async Task Email_TakenByOther_Kept()
    {
        _customers.Setup(x => x.GetCustomerByEmail("new@shop.com", It.IsAny<string>())).ReturnsAsync(new Customer { Id = "c2" });
        await Sync("new@shop.com");
        VerifyEmail("new@shop.com", Times.Never());
        Assert.AreEqual("old@shop.com", _customer.Email);
        _logger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("keeps old@shop.com") && v.ToString()!.Contains("customer c2")),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    [TestMethod]
    public async Task Email_CaseOnly_NotWritten()
    {
        _customer.Email = "user@shop.com";
        await Sync("User@Shop.com");
        VerifyEmail("user@shop.com", Times.Never());
        _customers.Verify(x => x.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Email_PerStoreIdentity_LooksUpInStore()
    {
        _config.RegisterCustomersPerStore = true;
        await Sync("new@shop.com");
        _customers.Verify(x => x.GetCustomerByEmail("new@shop.com", "s1"), Times.Once);
    }

    [TestMethod]
    public async Task Email_GlobalIdentity_LooksUpGlobally()
    {
        _config.RegisterCustomersPerStore = false;
        await Sync("new@shop.com");
        _customers.Verify(x => x.GetCustomerByEmail("new@shop.com", ""), Times.Once);
    }
}
