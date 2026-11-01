using Grand.Business.Core.Utilities.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Authentication.Tests.Services.Scim;

[TestClass]
public class ScimAccountLockTests
{
    [TestMethod]
    public async Task ResetCredentials_ChangesThePasswordInTheStoreOfTheAccount()
    {
        var fixture = new ScimFixture();
        var customer = await fixture.AddCustomer("jan@example.com");
        customer.StoreId = "store-9";

        await fixture.AccountLock().ResetCredentials(customer);

        fixture.CustomerManager.Verify(x => x.ChangePassword(
            It.Is<ChangePasswordRequest>(r => r.Email == "jan@example.com"), "store-9"), Times.Once);
    }
}
