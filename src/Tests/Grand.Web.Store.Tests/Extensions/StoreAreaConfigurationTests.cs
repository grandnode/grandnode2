using Grand.Web.Common.Controllers;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Store.Tests.Extensions;

[TestClass]
public class StoreAreaConfigurationTests
{
    /// <summary>
    ///     A provider coming from this test assembly - which ships <see cref="FakeStoreController" />
    ///     below - stands in for a plugin that has a Store-area configuration screen.
    /// </summary>
    private class ProviderWithStoreArea;

    [TestMethod]
    public void Exists_ProviderFromAssemblyWithStoreAreaController_ReturnTrue()
    {
        Assert.IsTrue(StoreAreaConfiguration.Exists(new ProviderWithStoreArea()));
    }

    [TestMethod]
    public void Exists_ProviderWithoutStoreAreaController_ReturnFalse()
    {
        //a mocked interface lives in the dynamic proxy assembly, which ships no controllers at all
        Assert.IsFalse(StoreAreaConfiguration.Exists(Mock.Of<IDisposable>()));
    }

    [TestMethod]
    public void Exists_NullProvider_ReturnFalse()
    {
        Assert.IsFalse(StoreAreaConfiguration.Exists(null));
    }

    [TestMethod]
    public void GetConfigurationUrl_ProviderFromAssemblyWithStoreAreaController_ReturnStoreAreaUrl()
    {
        Assert.AreEqual("/Store/FakeStore/Configure",
            StoreAreaConfiguration.GetConfigurationUrl(new ProviderWithStoreArea()));
    }

    [TestMethod]
    public void GetConfigurationUrl_ProviderWithoutStoreAreaController_ReturnNull()
    {
        Assert.IsNull(StoreAreaConfiguration.GetConfigurationUrl(Mock.Of<IDisposable>()));
    }

    [TestMethod]
    public void GetConfigurationUrl_NullProvider_ReturnNull()
    {
        Assert.IsNull(StoreAreaConfiguration.GetConfigurationUrl(null));
    }
}

/// <summary>
///     Stands in for the configuration screen a multi-store capable plugin ships. It is the only
///     Store-area controller in this test assembly, so the url built from it is deterministic.
///     The two Configure overloads mirror a real screen: a shipped controller always has both a GET
///     and a POST, and looking the action up by name alone would throw on that ambiguity.
/// </summary>
[Area("Store")]
public class FakeStoreController : BaseController
{
    public IActionResult Configure()
    {
        return new EmptyResult();
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public IActionResult Configure(string model)
    {
        return new EmptyResult();
    }
}
