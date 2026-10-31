using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Messages.Tests.DotLiquidDrops;

[TestClass]
public class LiquidOrderTests
{
    private static LiquidOrder Liquid(Order order)
    {
        return new LiquidOrder(order, new Customer(), new Language(), new Currency(),
            new Store { Url = "https://shop.test/" }, null);
    }

    [TestMethod]
    public void PurchaseOrderNumber_IsHtmlEncoded()
    {
        Assert.AreEqual("&lt;b&gt;x&lt;/b&gt;", Liquid(new Order { PurchaseOrderNumber = "<b>x</b>" }).PurchaseOrderNumber);
    }

    [TestMethod]
    public void PaymentDueDate_FormattedAsCalendarDate()
    {
        var due = new DateTime(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc);

        Assert.AreEqual(due.ToString("D"), Liquid(new Order { PaymentDueDateUtc = due }).PaymentDueDate);
    }

    [TestMethod]
    public void PaymentDueDate_EmptyWhenNotOnCredit()
    {
        var liquid = Liquid(new Order());

        Assert.AreEqual("", liquid.PaymentDueDate);
        Assert.AreEqual("", liquid.PurchaseOrderNumber);
    }
}
