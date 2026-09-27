using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Messages.Tests.DotLiquidDrops;

[TestClass]
public class LiquidCustomerTests
{
    private static Customer CustomerWithToken(string area = null)
    {
        var customer = new Customer { Email = "a+b@example.com" };
        customer.UserFields.Add(new UserField
            { Key = SystemCustomerFieldNames.PasswordRecoveryToken, Value = "tok", StoreId = "" });
        if (area != null)
            customer.UserFields.Add(new UserField
                { Key = SystemCustomerFieldNames.PasswordRecoveryArea, Value = area, StoreId = "" });
        return customer;
    }

    [TestMethod]
    public void PasswordRecoveryURL_NoArea_LeadsToStorefront()
    {
        var liquid = new LiquidCustomer(CustomerWithToken(), new Store { Url = "https://shop.test/" }, null);

        Assert.AreEqual("https://shop.test/passwordrecovery/confirm?token=tok&email=a%2Bb%40example.com",
            liquid.PasswordRecoveryURL);
    }

    [TestMethod]
    public void PasswordRecoveryURL_PanelArea_LeadsToThatPanel()
    {
        var liquid = new LiquidCustomer(CustomerWithToken("vendor"), new Store { Url = "https://shop.test/" }, null);

        Assert.AreEqual("https://shop.test/vendor/passwordrecovery/confirm?token=tok&email=a%2Bb%40example.com",
            liquid.PasswordRecoveryURL);
    }
}
