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
    private Mock<IAuditRecordReader> _reader;
    private AuditReferences _references;

    [TestInitialize]
    public void Init()
    {
        _reader = new Mock<IAuditRecordReader>();
        _reader.Setup(r => r.Read(It.IsAny<Type>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns<Type, string, string, string>((type, value, key, _) =>
                Task.FromResult(_records.GetValueOrDefault((type, key == null ? value : $"{key}={value}"))));
        _references = new AuditReferences(_reader.Object);
    }

    private async Task<AuditChange> Describe(Type type, string path, string oldValue, string newValue,
        IDictionary<string, string> after = null, bool truncated = false)
    {
        var change = new AuditChange { Path = path, OldValue = oldValue, NewValue = newValue, Truncated = truncated };
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
    public async Task AddressState_FoundInItsCountry()
    {
        _records[(typeof(Grand.Domain.Directory.Country), "pl")] = new Dictionary<string, string> {
            ["Name"] = "Poland", ["StateProvinces[id=st1].Name"] = "Mazowieckie"
        };
        var order = new Dictionary<string, string> { ["BillingAddress.CountryId"] = "pl" };

        var country = await Describe(typeof(Order), "BillingAddress.CountryId", null, "pl", order);
        var state = await Describe(typeof(Order), "BillingAddress.StateProvinceId", null, "st1", order);

        Assert.AreEqual("Poland (pl)", country.NewValue);
        Assert.AreEqual("Mazowieckie (st1)", state.NewValue);
    }

    [TestMethod]
    public async Task ListOfIds_EachNamed()
    {
        _records[(typeof(Store), "s1")] = new Dictionary<string, string> { ["Name"] = "Main" };

        var change = await Describe(typeof(Product), "Stores", null, "[\"s1\", \"s9\"]");

        Assert.AreEqual("Main (s1), s9", change.NewValue);
    }

    [TestMethod]
    public async Task ReferenceByKey_MatchesThatProperty()
    {
        _records[(typeof(OrderStatus), "StatusId=10")] = new Dictionary<string, string> { ["Name"] = "Pending" };
        _records[(typeof(OrderStatus), "StatusId=40")] = new Dictionary<string, string> { ["Name"] = "Cancelled" };

        var change = await Describe(typeof(Order), "OrderStatusId", "10", "40");

        Assert.AreEqual("Pending (10)", change.OldValue);
        Assert.AreEqual("Cancelled (40)", change.NewValue);
    }

    [TestMethod]
    public async Task EnumProperty_ShowsName()
    {
        var change = await Describe(typeof(Order), "PaymentStatusId", "10", "30");

        Assert.AreEqual("Pending (10)", change.OldValue);
        Assert.AreEqual("Paid (30)", change.NewValue);
    }

    //a wrong mark shows only ids with no sign of the mistake, so every mark of the domain is checked here
    [TestMethod]
    public void EveryReferenceMark_PointsToANamedRecord()
    {
        var problems = new List<string>();
        foreach (var type in typeof(Grand.Domain.BaseEntity).Assembly.GetTypes())
        foreach (var property in type.GetProperties())
        {
            if (property.GetCustomAttributes(typeof(AuditReferenceAttribute), false).FirstOrDefault() is not AuditReferenceAttribute mark)
                continue;

            var name = $"{type.Name}.{property.Name}";
            var target = mark.Target;
            var isList = property.PropertyType != typeof(string)
                         && typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType);
            if (target.IsEnum)
                continue;
            if (!typeof(Grand.Domain.BaseEntity).IsAssignableFrom(target))
                problems.Add($"{name}: {target.Name} is not an entity");
            if (property.PropertyType != typeof(string) && !isList && mark.Key == null)
                problems.Add($"{name}: a {property.PropertyType.Name} value needs a Key");
            if (mark.Key != null && target.GetProperty(mark.Key) == null)
                problems.Add($"{name}: {target.Name} has no {mark.Key}");
            if (mark.Owner != null && type.GetProperty(mark.Owner) == null)
                problems.Add($"{name}: {type.Name} has no owner property {mark.Owner}");

            var named = target;
            if (mark.List != null)
            {
                var list = target.GetProperty(mark.List);
                named = list?.PropertyType.GetGenericArguments().FirstOrDefault();
                if (named == null)
                    problems.Add($"{name}: {target.Name} has no list {mark.List}");
                if (mark.Owner == null)
                    problems.Add($"{name}: an element of a list needs an Owner");
            }

            if (named != null && named.GetProperty(AuditReferences.NameProperty(named)) == null)
                problems.Add($"{name}: {named.Name} has no name property");
        }

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    [TestMethod]
    public async Task Reference_ReadsOnlyTheNameField()
    {
        _records[(typeof(Brand), "b1")] = new Dictionary<string, string> { ["Name"] = "Aurora" };

        await Describe(typeof(Product), "BrandId", null, "b1");

        _reader.Verify(r => r.Read(typeof(Brand), "b1", null, "Name"), Times.Once);
    }

    [TestMethod]
    public async Task TruncatedValue_KeepsTheStoredValue()
    {
        _records[(typeof(Store), "s1")] = new Dictionary<string, string> { ["Name"] = "Main" };

        var change = await Describe(typeof(Product), "Stores", null, "[\"s1\", \"s", truncated: true);

        Assert.AreEqual("[\"s1\", \"s", change.NewValue);
        Assert.IsTrue(change.Truncated);
        _reader.Verify(r => r.Read(It.IsAny<Type>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task LongNames_CutToTheLimitAndMarked()
    {
        var ids = Enumerable.Range(0, 40).Select(i => $"s{i}").ToList();
        foreach (var id in ids)
            _records[(typeof(Store), id)] = new Dictionary<string, string> { ["Name"] = new string('n', 40) };

        var change = await Describe(typeof(Product), "Stores", null,
            "[" + string.Join(", ", ids.Select(id => $"\"{id}\"")) + "]");

        Assert.AreEqual(AuditDiff.MaxValueLength, change.NewValue.Length);
        Assert.IsTrue(change.Truncated);
    }

    private async Task<IList<string>> ElementNames(string path, IDictionary<string, string> before,
        IDictionary<string, string> after)
    {
        var change = new AuditChange { Path = path };
        await _references.NameElements(typeof(Product), [change], before, after);
        return change.ElementNames;
    }

    [TestMethod]
    public async Task Element_NamedByTheRecordItRefersTo()
    {
        _records[(typeof(SpecificationAttribute), "s1")] = new Dictionary<string, string> { ["Name"] = "Color family" };
        var product = new Dictionary<string, string> { ["ProductSpecificationAttributes[id=p1].SpecificationAttributeId"] = "s1" };

        var names = await ElementNames("ProductSpecificationAttributes[id=p1].DisplayOrder", product, product);

        CollectionAssert.AreEqual(new[] { "Color family" }, names.ToArray());
    }

    [TestMethod]
    public async Task NestedElements_EachNamed()
    {
        _records[(typeof(ProductAttribute), "pa1")] = new Dictionary<string, string> { ["Name"] = "Color" };
        var product = new Dictionary<string, string> {
            ["ProductAttributeMappings[id=m].ProductAttributeId"] = "pa1",
            ["ProductAttributeMappings[id=m].ProductAttributeValues[id=v].Name"] = "Red"
        };

        var names = await ElementNames("ProductAttributeMappings[id=m].ProductAttributeValues[id=v].PriceAdjustment",
            product, product);

        CollectionAssert.AreEqual(new[] { "Color", "Red" }, names.ToArray());
    }

    [TestMethod]
    public async Task RemovedElement_NamedFromBefore()
    {
        _records[(typeof(Category), "c1")] = new Dictionary<string, string> { ["Name"] = "Lamps" };
        var before = new Dictionary<string, string> { ["ProductCategories[id=x].CategoryId"] = "c1" };

        var names = await ElementNames("ProductCategories[id=x].DisplayOrder", before, new Dictionary<string, string>());

        CollectionAssert.AreEqual(new[] { "Lamps" }, names.ToArray());
    }

    [TestMethod]
    public async Task ElementWithoutName_NoNames()
    {
        var product = new Dictionary<string, string> { ["TierPrices[id=t].Quantity"] = "5" };

        Assert.IsNull(await ElementNames("TierPrices[id=t].Quantity", null, product));
        Assert.IsNull(await ElementNames("Name", null, product));
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
