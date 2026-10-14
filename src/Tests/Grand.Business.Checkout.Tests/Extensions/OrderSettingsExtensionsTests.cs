using Grand.Business.Core.Extensions;
using Grand.Domain.Orders;
using Grand.Infrastructure.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Extensions;

[TestClass]
public class OrderSettingsExtensionsTests
{
    [TestMethod]
    [DataRow(true, OidcSignInMode.Mixed, true)]
    [DataRow(false, OidcSignInMode.Mixed, false)]
    [DataRow(true, OidcSignInMode.ProviderOnly, false)]
    [DataRow(false, OidcSignInMode.ProviderOnly, false)]
    public void AnonymousCheckoutAllowed(bool stored, OidcSignInMode mode, bool expected) =>
        Assert.AreEqual(expected, new OrderSettings { AnonymousCheckoutAllowed = stored }
            .AnonymousCheckoutAllowed(new OidcLoginConfig { SignInMode = mode }));

    [TestMethod]
    public void NullConfig_StoredValue() =>
        Assert.IsTrue(new OrderSettings { AnonymousCheckoutAllowed = true }.AnonymousCheckoutAllowed(null));
}
