using Grand.Domain.Customers;
using Grand.Web.Common.Security.ExternalRegistration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Common.Tests.Security.ExternalRegistration;

[TestClass]
public class ExternalRegistrationPolicyTests
{
    [TestMethod]
    [DataRow(UserRegistrationType.Standard, true, UserRegistrationType.Standard)]
    [DataRow(UserRegistrationType.EmailValidation, true, UserRegistrationType.Standard)]
    [DataRow(UserRegistrationType.AdminApproval, true, UserRegistrationType.AdminApproval)]
    [DataRow(UserRegistrationType.Standard, false, UserRegistrationType.EmailValidation)]
    [DataRow(UserRegistrationType.EmailValidation, false, UserRegistrationType.EmailValidation)]
    [DataRow(UserRegistrationType.AdminApproval, false, UserRegistrationType.AdminApproval)]
    [DataRow(UserRegistrationType.Disabled, true, UserRegistrationType.Disabled)]
    public void EffectiveType(UserRegistrationType store, bool verified, UserRegistrationType expected) =>
        Assert.AreEqual(expected, ExternalRegistrationPolicy.EffectiveRegistrationType(store, verified));
}
