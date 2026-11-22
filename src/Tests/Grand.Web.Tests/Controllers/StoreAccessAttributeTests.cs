using Grand.Web.Common.Filters;
using Grand.Web.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Grand.Web.Tests.Controllers;

/// <summary>
///     Actions reached from the sign-in screens of a store the customer may not browse
/// </summary>
[TestClass]
public class StoreAccessAttributeTests
{
    private static void AssertOpenToGuestsOfAClosedStore(MethodInfo action)
    {
        Assert.IsTrue(action.GetCustomAttribute<PublicStoreAttribute>()?.IgnoreFilter ?? false,
            $"{action.Name} needs [PublicStore(true)]");
        Assert.IsTrue(action.GetCustomAttribute<ClosedStoreAttribute>()?.IgnoreFilter ?? false,
            $"{action.Name} needs [ClosedStore(true)]");
    }

    [TestMethod]
    public void TwoFactorAuthorization_OpenLikeLogin()
    {
        var actions = typeof(AccountController).GetMethods()
            .Where(m => m.Name == nameof(AccountController.TwoFactorAuthorization)).ToList();

        Assert.AreEqual(2, actions.Count);
        actions.ForEach(AssertOpenToGuestsOfAClosedStore);
    }

    [TestMethod]
    public void SetLanguage_OpenOnAClosedStore()
    {
        AssertOpenToGuestsOfAClosedStore(typeof(CommonController).GetMethod(nameof(CommonController.SetLanguage)));
    }
}
