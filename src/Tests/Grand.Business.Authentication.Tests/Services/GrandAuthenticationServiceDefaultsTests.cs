using Grand.Business.Core.Interfaces.Authentication;
using Grand.Domain.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class GrandAuthenticationServiceDefaultsTests
{
    [TestMethod]
    public async Task HasOidcSession_ImplementationWithoutIt_ReturnsFalse()
    {
        IGrandAuthenticationService service = new LegacyAuthenticationService();

        Assert.IsFalse(await service.HasOidcSession());
    }

    //an implementation written before HasOidcSession existed
    private sealed class LegacyAuthenticationService : IGrandAuthenticationService
    {
        public Task SignIn(Customer customer, bool createPersistentCookie) => Task.CompletedTask;

        public Task SignIn(Customer customer, bool createPersistentCookie, string oidcSessionId) => Task.CompletedTask;

        public Task SignOut() => Task.CompletedTask;

        public Task<Customer> GetAuthenticatedCustomer() => Task.FromResult<Customer>(null);

        public Task<string> GetCustomerGuid() => Task.FromResult<string>(null);

        public Task SetCustomerGuid(Guid customerGuid) => Task.CompletedTask;
    }
}
