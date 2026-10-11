using Grand.Infrastructure.Configuration;
using Grand.Module.Api.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Module.Api.Tests.Infrastructure;

[TestClass]
public class ApiOidcDisabledPathProviderTests
{
    [TestMethod]
    public void ApiDisabled_ApiPathWithdrawn() =>
        CollectionAssert.AreEqual(new[] { "/api" },
            new ApiOidcDisabledPathProvider(new BackendAPIConfig { Enabled = false }).GetDisabledPaths().ToList());

    [TestMethod]
    public void ApiEnabled_NothingWithdrawn() =>
        Assert.AreEqual(0,
            new ApiOidcDisabledPathProvider(new BackendAPIConfig { Enabled = true }).GetDisabledPaths().Count());
}
