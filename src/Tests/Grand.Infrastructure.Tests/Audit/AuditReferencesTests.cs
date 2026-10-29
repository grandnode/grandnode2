using Grand.Data.Audit;
using Grand.Domain.Audit;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Infrastructure.Audit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Infrastructure.Tests.Audit;

[TestClass]
public class AuditReferencesTests
{
    private readonly Dictionary<(Type, string), IDictionary<string, string>> _records = new();
    private AuditReferences _references;

    [TestInitialize]
    public void Init()
    {
        var reader = new Mock<IAuditRecordReader>();
        reader.Setup(r => r.Read(It.IsAny<Type>(), It.IsAny<string>()))
            .Returns<Type, string>((type, id) => Task.FromResult(_records.GetValueOrDefault((type, id))));
        _references = new AuditReferences(reader.Object);
    }

    private async Task<AuditChange> Describe(Type type, string path, string oldValue, string newValue,
        IDictionary<string, string> after = null)
    {
        var change = new AuditChange { Path = path, OldValue = oldValue, NewValue = newValue };
        await _references.Describe(type, [change], after, after);
        return change;
    }

    [TestMethod]
    public async Task Reference_ShowsNameAndId()
    {
        _records[(typeof(Brand), "b1")] = new Dictionary<string, string> { ["Name"] = "Aurora" };
        _records[(typeof(Brand), "b2")] = new Dictionary<string, string> { ["Name"] = "Nordic" };

        var change = await Describe(typeof(Product), "BrandId", "b1", "b2");

        Assert.AreEqual("Aurora (b1)", change.OldValue);
        Assert.AreEqual("Nordic (b2)", change.NewValue);
    }

    [TestMethod]
    public async Task ListElement_FoundInTheOwnerNamedByTheSibling()
    {
        _records[(typeof(SpecificationAttribute), "s1")] = new Dictionary<string, string> {
            ["Name"] = "Color",
            ["SpecificationAttributeOptions[id=o1].Name"] = "Red",
            ["SpecificationAttributeOptions[id=o2].Name"] = "Blue"
        };
        var product = new Dictionary<string, string> { ["ProductSpecificationAttributes[id=p1].SpecificationAttributeId"] = "s1" };

        var change = await Describe(typeof(Product), "ProductSpecificationAttributes[id=p1].SpecificationAttributeOptionId",
            "o1", "o2", product);

        Assert.AreEqual("Red (o1)", change.OldValue);
        Assert.AreEqual("Blue (o2)", change.NewValue);
    }

    [TestMethod]
    public async Task ListOfIds_EachNamed()
    {
        _records[(typeof(Store), "s1")] = new Dictionary<string, string> { ["Name"] = "Main" };

        var change = await Describe(typeof(Product), "Stores", null, "[\"s1\", \"s9\"]");

        Assert.AreEqual("Main (s1), s9", change.NewValue);
    }

    [TestMethod]
    public async Task EnumProperty_ShowsName()
    {
        var change = await Describe(typeof(Order), "PaymentStatusId", "10", "30");

        Assert.AreEqual("Pending (10)", change.OldValue);
        Assert.AreEqual("Paid (30)", change.NewValue);
    }

    [TestMethod]
    public async Task UnmarkedOrMissing_KeepsTheStoredValue()
    {
        var unmarked = await Describe(typeof(Product), "Name", "a", "b");
        var missing = await Describe(typeof(Product), "BrandId", null, "b9");

        Assert.AreEqual("b", unmarked.NewValue);
        Assert.AreEqual("b9", missing.NewValue);
    }
}
