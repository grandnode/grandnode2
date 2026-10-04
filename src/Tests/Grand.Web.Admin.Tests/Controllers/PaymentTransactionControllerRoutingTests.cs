using Grand.Web.Admin.Controllers;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class PaymentTransactionControllerRoutingTests
{
    [TestMethod]
    public void AdminPaymentTransactionController_InheritsBasePaymentTransactionController() =>
        Assert.IsTrue(typeof(BasePaymentTransactionController).IsAssignableFrom(typeof(PaymentTransactionController)));

    [TestMethod]
    public void AdminPaymentTransactionController_HasAutoValidateAntiforgeryToken() =>
        Assert.IsTrue(typeof(PaymentTransactionController)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute), false)
            .Length > 0);

    [TestMethod]
    public void AdminPaymentTransactionController_HasAreaAttributeWithAdminArea()
    {
        var areaAttr = (AreaAttribute)Attribute.GetCustomAttribute(typeof(PaymentTransactionController), typeof(AreaAttribute), false);
        Assert.IsNotNull(areaAttr, "Missing [Area].");
        Assert.AreEqual(Constants.AreaAdmin, areaAttr.RouteValue);
    }

    [TestMethod]
    public void AdminPaymentTransactionController_HasAuthorizeAdminAttribute() =>
        Assert.IsTrue(typeof(PaymentTransactionController).IsDefined(typeof(AuthorizeAdminAttribute), false),
            "Missing [AuthorizeAdmin].");
}
