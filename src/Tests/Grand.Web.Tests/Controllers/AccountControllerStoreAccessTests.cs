using Grand.Web.Common.Filters;
using Grand.Web.Common.Models;
using Grand.Web.Controllers;
using Grand.Web.Models.Customer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Tests.Controllers;

[TestClass]
public class AccountControllerStoreAccessTests
{
    [TestMethod]
    public void AccountController_HasStoreAccessFilter()
    {
        Assert.IsTrue(typeof(AccountController).GetCustomAttributes(typeof(StoreAccessAttribute), true).Length > 0);
    }

    [TestMethod]
    public void OidcLoginController_HasStoreAccessFilter()
    {
        //its link and failure screens are reachable by guests of a store they may not browse
        Assert.IsTrue(typeof(OidcLoginController).GetCustomAttributes(typeof(StoreAccessAttribute), true).Length > 0);
    }

    [TestMethod]
    public void GuestAccountModels_CarryStoreAccess()
    {
        Type[] models = [
            typeof(LoginModel), typeof(RegisterModel), typeof(RegisterResultModel), typeof(PasswordRecoveryModel),
            typeof(PasswordRecoveryConfirmModel), typeof(AccountActivationModel), typeof(TwoFactorLoginModel),
            typeof(OidcLinkModel), typeof(OidcFailedModel)
        ];
        foreach (var model in models)
            Assert.IsTrue(typeof(IStoreAccessModel).IsAssignableFrom(model), model.Name);
    }
}
