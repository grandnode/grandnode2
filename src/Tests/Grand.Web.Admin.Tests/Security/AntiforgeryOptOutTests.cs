using Grand.Web.Admin.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Grand.Web.Admin.Tests.Security;

[TestClass]
public class AntiforgeryOptOutTests
{
    /// <summary>
    ///     BaseAdminController carries [AutoValidateAntiforgeryToken]; a single [IgnoreAntiforgeryToken]
    ///     opens the action to CSRF from an authenticated administrator's browser. The panel has no
    ///     endpoint that needs the opt-out - every caller already sends the token, either as the
    ///     __RequestVerificationToken field or as the X-CSRF-TOKEN header.
    /// </summary>
    [TestMethod]
    public void AdminControllers_DoNotOptOutOfAntiforgery()
    {
        var offenders = (from controller in typeof(BaseAdminController).Assembly.GetTypes()
                where typeof(ControllerBase).IsAssignableFrom(controller) && !controller.IsAbstract
                from action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance |
                                                     BindingFlags.DeclaredOnly)
                where !action.IsSpecialName &&
                      (action.IsDefined(typeof(IgnoreAntiforgeryTokenAttribute), true) ||
                       controller.IsDefined(typeof(IgnoreAntiforgeryTokenAttribute), true))
                select $"{controller.Name}.{action.Name}")
            .Distinct()
            .ToList();

        Assert.AreEqual(0, offenders.Count,
            $"Antiforgery validation is disabled on: {string.Join(", ", offenders)}");
    }
}
