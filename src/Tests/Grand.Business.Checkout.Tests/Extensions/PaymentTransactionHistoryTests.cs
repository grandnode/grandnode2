using Grand.Business.Checkout.Extensions;
using Grand.Domain.Payments;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Extensions;

[TestClass]
public class PaymentTransactionHistoryTests
{
    [TestMethod]
    public void ToOrderNote_AllParts()
    {
        var entry = new PaymentTransaction().AddEntry(PaymentTransactionEntryType.Payment, 1250, "staff@shop.com",
            "TR-881", "First instalment");

        Assert.AreEqual("Payment of 1,250.00 EUR recorded by staff@shop.com. Reference: TR-881. Note: First instalment",
            entry.ToOrderNote("EUR"));
    }

    [TestMethod]
    public void ToOrderNote_WithoutCreatedBy_SaysRecorded()
    {
        var entry = new PaymentTransaction().AddEntry(PaymentTransactionEntryType.Refund, 5, null);

        Assert.AreEqual("Refund of 5.00 USD recorded", entry.ToOrderNote("USD"));
    }

    [TestMethod]
    public void AddEntry_BlankReference_StoresNull()
    {
        var transaction = new PaymentTransaction();

        var entry = transaction.AddEntry(PaymentTransactionEntryType.Payment, 10, " staff@shop.com ", "  ", "");

        Assert.AreEqual(1, transaction.Entries.Count);
        Assert.AreSame(entry, transaction.Entries[0]);
        Assert.AreEqual("staff@shop.com", entry.CreatedBy);
        Assert.IsNull(entry.Reference);
        Assert.IsNull(entry.Note);
        Assert.IsTrue(entry.CreatedOnUtc > DateTime.UtcNow.AddMinutes(-1));
    }
}
