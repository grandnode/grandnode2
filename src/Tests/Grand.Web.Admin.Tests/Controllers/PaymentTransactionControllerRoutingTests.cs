using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Controllers;
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
}
