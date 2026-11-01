using Grand.Domain.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Authentication.Tests.Services.Scim;

[TestClass]
public class ScimAccountLockTests
{
    [TestMethod]
    public async Task ResetCredentials_ChangesThePasswordOfThatAccount()
    {
        var fixture = new ScimFixture();
        var customer = await fixture.AddCustomer("jan@example.com");

        await fixture.AccountLock().ResetCredentials(customer);

        fixture.CustomerManager.Verify(x => x.ChangePassword(customer, PasswordFormat.Hashed, It.IsAny<string>()),
            Times.Once);
    }
}
