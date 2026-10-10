using Grand.Business.Authentication.Services;
using Grand.Web.Common.Security.Oidc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Common.Tests.Security.Oidc;

[TestClass]
public class OidcProviderNameTests
{
    //the link written by the resolver must be the one the web layer and the login plugin look up
    [TestMethod]
    public void ResolverAndWebDefaults_UseTheSameLinkName() =>
        Assert.AreEqual(OidcCustomerResolver.ProviderSystemName, OidcResourceServerDefaults.ProviderSystemName);
}
