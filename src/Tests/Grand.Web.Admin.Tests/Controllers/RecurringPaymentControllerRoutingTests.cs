using Grand.Web.Admin.Controllers;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class RecurringPaymentControllerRoutingTests
{
    [TestMethod]
    public void AdminRecurringPaymentController_InheritsBaseRecurringPaymentController() =>
        Assert.IsTrue(typeof(BaseRecurringPaymentController).IsAssignableFrom(typeof(RecurringPaymentController)));

    [TestMethod]
    public void AdminRecurringPaymentController_HasAutoValidateAntiforgeryToken() =>
        Assert.IsTrue(typeof(RecurringPaymentController)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute), false)
            .Length > 0);

    [TestMethod]
    public void AdminRecurringPaymentController_HasAreaAttributeWithAdminArea()
    {
        var areaAttr = (AreaAttribute)Attribute.GetCustomAttribute(typeof(RecurringPaymentController), typeof(AreaAttribute), false);
        Assert.IsNotNull(areaAttr, "Missing [Area].");
        Assert.AreEqual(Constants.AreaAdmin, areaAttr.RouteValue);
    }

    [TestMethod]
    public void AdminRecurringPaymentController_HasAuthorizeAdminAttribute() =>
        Assert.IsTrue(typeof(RecurringPaymentController).IsDefined(typeof(AuthorizeAdminAttribute), false),
            "Missing [AuthorizeAdmin].");
}
