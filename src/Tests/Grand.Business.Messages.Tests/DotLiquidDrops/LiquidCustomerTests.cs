using Grand.Domain.Messages;
using Grand.Business.Messages.DotLiquidDrops;
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

    [TestMethod]
    public void TextEnteredByCustomer_IsHtmlEncoded()
    {
        var customer = new Customer { Email = "a@example.com" };
        customer.UserFields.Add(new UserField
            { Key = SystemCustomerFieldNames.FirstName, Value = "<img src=x onerror=alert(1)>", StoreId = "" });
        var liquid = new LiquidCustomer(customer, new Store { Url = "https://shop.test/" }, null);

        Assert.AreEqual("&lt;img src=x onerror=alert(1)&gt;", liquid.FirstName);
        Assert.DoesNotContain("<", liquid.FullName);
    }

    [TestMethod]
    public void RenderSubject_IsPlainText_WhileBodyIsEncoded()
    {
        var customer = new Customer();
        customer.UserFields.Add(new UserField
            { Key = SystemCustomerFieldNames.FirstName, Value = "O'Brien & Co", StoreId = "" });
        var liquidObject = new LiquidObject { Customer = new LiquidCustomer(customer, new Store { Url = "https://shop.test/" }, null) };

        Assert.AreEqual("Hi O&#39;Brien &amp; Co", LiquidExtensions.Render(liquidObject, "Hi {{Customer.FirstName}}"));
        Assert.AreEqual("Hi O'Brien & Co", LiquidExtensions.RenderSubject(liquidObject, "Hi {{Customer.FirstName}}"));
    }
}
