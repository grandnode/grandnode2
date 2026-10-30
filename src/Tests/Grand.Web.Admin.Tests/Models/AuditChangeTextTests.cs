using Grand.Domain.Audit;
using Grand.Web.Admin.Models.AuditLog;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Models;

[TestClass]
public class AuditChangeTextTests
{
    private static readonly Dictionary<string, string> Resources = new() {
        ["Admin.Common.Yes"] = "Yes",
        ["Admin.Common.No"] = "No",
        ["Admin.System.AuditLog.Added"] = "added",
        ["Admin.System.AuditLog.Removed"] = "removed",
        ["Admin.System.AuditLog.ChangedHidden"] = "changed (hidden)"
    };

    private readonly AuditChangeText _text = new(key => Resources.GetValueOrDefault(key, key));

    private static AuditChange Change(string path, string oldValue, string newValue, params string[] elementNames)
    {
        return new AuditChange {
            Path = path, OldValue = oldValue, NewValue = newValue,
            ElementNames = elementNames.Length == 0 ? null : elementNames
        };
    }

    private static List<AuditChange> AddedSpecification()
    {
        const string element = "ProductSpecificationAttributes[id=6ae42d82f6567f2f29c48cf7]";
        return [
            Change($"{element}.AttributeTypeId", null, "Option (0)", "Color family"),
            Change($"{element}.SpecificationAttributeId", null, "Color family (6ae399b7577fba63f26014ac)", "Color family"),
            Change($"{element}.SpecificationAttributeOptionId", null, "Black (6ae399b7577fba63f26014ad)", "Color family"),
            Change($"{element}.AllowFiltering", null, "false", "Color family"),
            Change($"{element}.ShowOnProductPage", null, "true", "Color family"),
            Change($"{element}.DisplayOrder", null, "0", "Color family")
        ];
    }

    [TestMethod]
    public void AddedElement_OneLineNamedByTheElement()
    {
        Assert.AreEqual(
            "Specification attributes › Color family: added (Attribute type: Option, Specification attribute option: Black, " +
            "Allow filtering: No, Show on product page: Yes, Display order: 0)",
            _text.Text("Product", AddedSpecification()));
    }

    [TestMethod]
    public void AddedElement_SummaryWithoutValues()
    {
        Assert.AreEqual("Specification attributes › Color family (added)",
            _text.Summary("Product", AddedSpecification()));
    }

    [TestMethod]
    public void ChangedElement_FieldsOfTheElement()
    {
        var changes = new List<AuditChange> {
            Change("ProductSpecificationAttributes[id=a].DisplayOrder", "0", "2", "Color family"),
            Change("ProductSpecificationAttributes[id=a].AllowFiltering", "false", "true", "Color family"),
            Change("Name", "Lamp", "Desk lamp")
        };

        Assert.AreEqual("Specification attributes › Color family: Display order: 0 → 2, Allow filtering: No → Yes · " +
                        "Name: Lamp → Desk lamp", _text.Text("Product", changes));
        Assert.AreEqual("Specification attributes › Color family: Display order, Allow filtering · Name",
            _text.Summary("Product", changes));
    }

    [TestMethod]
    public void RemovedElement_OnlyItsName()
    {
        var changes = new List<AuditChange> {
            Change("ProductCategories[id=c].CategoryId", "Lamps (c1)", null, "Lamps"),
            Change("ProductCategories[id=c].DisplayOrder", "1", null, "Lamps")
        };

        Assert.AreEqual("Categories › Lamps: removed", _text.Text("Product", changes));
    }

    [TestMethod]
    public void NestedElement_BothNamed()
    {
        var change = Change("ProductAttributeMappings[id=m].ProductAttributeValues[id=v].PriceAdjustment", "0", "5",
            "Color", "Red");

        Assert.AreEqual("Attribute mappings › Color › Attribute values › Red › Price adjustment",
            _text.Label("Product", change));
    }

    [TestMethod]
    public void UnnamedElement_NoId()
    {
        //an entry recorded before element names, or an element with nothing to name it by
        var changes = new List<AuditChange> {
            Change("TierPrices[id=6ae42d82f6567f2f29c48cf7].Quantity", null, "5"),
            Change("TierPrices[id=6ae42d82f6567f2f29c48cf7].Price", null, "9.5")
        };

        Assert.AreEqual("Tier prices: added (Quantity: 5, Price: 9.5)", _text.Text("Product", changes));
    }

    [TestMethod]
    public void KeyedElement_NamedByItsKey()
    {
        var changes = new List<AuditChange> { Change("UserFields[FirstName].Value", "Ann", "Anna") };

        Assert.AreEqual("User fields › FirstName: Value: Ann → Anna", _text.Text("Customer", changes));
    }

    [TestMethod]
    public void NestedField_InWords()
    {
        var changes = new List<AuditChange> { Change("BillingAddress.FirstName", "Ann", "Anna") };

        Assert.AreEqual("Billing address › First name: Ann → Anna", _text.Text("Order", changes));
    }

    [TestMethod]
    public void MaskedField_ValuesHidden()
    {
        var changes = new List<AuditChange> { new() { Path = "Password", Masked = true } };

        Assert.AreEqual("Password: changed (hidden)", _text.Text("Customer", changes));
    }

    [TestMethod]
    public void Values_CutWithEllipsis()
    {
        var changes = new List<AuditChange> {
            Change("FullDescription", null, new string('x', 500)),
            new() { Path = "ShortDescription", NewValue = "cut when stored", Truncated = true }
        };

        var text = _text.Text("Product", changes);

        StringAssert.StartsWith(text, $"Full description: — → {new string('x', AuditChangeText.ValueLength)}… · ");
        StringAssert.EndsWith(text, "Short description: — → cut when stored…");
    }

    [TestMethod]
    public void LongText_CutWithEllipsis()
    {
        var changes = Enumerable.Range(0, 40).Select(i => Change($"Field{i}", "old value", "new value")).ToList();

        var text = _text.Text("Product", changes);

        Assert.IsTrue(text.Length <= AuditChangeText.TextLength + 1, text);
        StringAssert.EndsWith(text, "…");
    }

    [TestMethod]
    public void ValueNotFromAReference_KeepsParentheses()
    {
        var changes = new List<AuditChange> { Change("Name", "Lamp (old)", "Lamp (new)") };

        Assert.AreEqual("Name: Lamp (old) → Lamp (new)", _text.Text("Product", changes));
    }
}
