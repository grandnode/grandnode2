using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Utilities;

[TestClass]
public class PurchaseOrderNumberRulesTests
{
    [TestMethod]
    public void Normalize_Trims()
    {
        Assert.AreEqual("PO-1", PurchaseOrderNumberRules.Normalize("  PO-1 ", PurchaseOrderNumberMode.Optional));
    }

    [TestMethod]
    public void Normalize_Whitespace_Null()
    {
        Assert.IsNull(PurchaseOrderNumberRules.Normalize("   ", PurchaseOrderNumberMode.Optional));
    }

    [TestMethod]
    public void Normalize_Disabled_IgnoresInput()
    {
        Assert.IsNull(PurchaseOrderNumberRules.Normalize("PO-1", PurchaseOrderNumberMode.Disabled));
    }

    [TestMethod]
    public void Normalize_KeepsCase()
    {
        Assert.AreEqual("po-Ab", PurchaseOrderNumberRules.Normalize("po-Ab", PurchaseOrderNumberMode.Required));
    }

    [TestMethod]
    public void Validate_RequiredMissing_RequiredError()
    {
        Assert.AreEqual("Checkout.PurchaseOrderNumber.Required",
            PurchaseOrderNumberRules.Validate(null, PurchaseOrderNumberMode.Required));
    }

    [TestMethod]
    public void Validate_OptionalMissing_Valid()
    {
        Assert.IsNull(PurchaseOrderNumberRules.Validate(null, PurchaseOrderNumberMode.Optional));
    }

    [TestMethod]
    public void Validate_TooLong_TooLongError()
    {
        Assert.AreEqual("Checkout.PurchaseOrderNumber.TooLong",
            PurchaseOrderNumberRules.Validate(new string('x', 51), PurchaseOrderNumberMode.Optional));
    }

    [TestMethod]
    public void Validate_MaxLength_Valid()
    {
        Assert.IsNull(PurchaseOrderNumberRules.Validate(new string('x', 50), PurchaseOrderNumberMode.Required));
    }

    [TestMethod]
    public void Validate_Markup_StoredAsTyped()
    {
        Assert.IsNull(PurchaseOrderNumberRules.Validate("<script>", PurchaseOrderNumberMode.Optional));
    }
}
