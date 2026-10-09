using Grand.Web.Common.RequestCustomerResolvers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Common.Tests;

[TestClass]
public class RequestCustomerResolverRegistrationTests
{
    [TestMethod]
    public void AddRequestCustomerResolvers_RegistersExactlyTheBuiltInResolversAsScoped()
    {
        var services = new ServiceCollection();

        services.AddRequestCustomerResolvers();

        var registered = services.Where(d => d.ServiceType == typeof(IRequestCustomerResolver)).ToList();
        Assert.IsTrue(registered.All(d => d.Lifetime == ServiceLifetime.Scoped));
        CollectionAssert.AreEquivalent(new[]
        {
            typeof(AllowAnonymousCustomerResolver), typeof(CookieCustomerResolver),
            typeof(GuestCookieCustomerResolver), typeof(SearchEngineCustomerResolver),
            typeof(ApiUserCustomerResolver)
        }, registered.Select(d => d.ImplementationType).ToList());
    }
}
