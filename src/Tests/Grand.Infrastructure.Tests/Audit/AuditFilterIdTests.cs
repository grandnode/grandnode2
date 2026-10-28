using Grand.Data;
using Grand.Data.Audit;
using Grand.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.Audit;

[TestClass]
public class AuditFilterIdTests
{
    [TestMethod]
    public void TryGetId_ConstantEquality()
    {
        Assert.IsTrue(AuditFilterId.TryGetId<Thing>(x => x.Id == "42", out var id));
        Assert.AreEqual("42", id);
    }

    [TestMethod]
    public void TryGetId_CapturedVariable()
    {
        var thing = new Thing { Id = "7" };

        Assert.IsTrue(AuditFilterId.TryGetId<Thing>(x => x.Id == thing.Id, out var id));
        Assert.AreEqual("7", id);
    }

    [TestMethod]
    public void TryGetId_ReversedOperands()
    {
        var expected = "9";

        Assert.IsTrue(AuditFilterId.TryGetId<Thing>(x => expected == x.Id, out var id));
        Assert.AreEqual("9", id);
    }

    [TestMethod]
    public void TryGetId_OtherFilter_False()
    {
        Assert.IsFalse(AuditFilterId.TryGetId<Thing>(x => x.Name == "a", out _));
    }

    [TestMethod]
    public void TryGetId_CompoundFilter_False()
    {
        var id = "1";

        Assert.IsFalse(AuditFilterId.TryGetId<Thing>(x => x.Id == id && x.Published, out _));
    }

    [TestMethod]
    public void MemberPath_Simple()
    {
        Assert.AreEqual("Name", AuditFilterId.MemberPath<Thing, string>(x => x.Name));
    }

    [TestMethod]
    public void MemberPath_Nested()
    {
        Assert.AreEqual("Inner.Value", AuditFilterId.MemberPath<Thing, int>(x => x.Inner.Value));
    }

    [TestMethod]
    public void MemberPath_FromUpdateBuilder()
    {
        var builder = UpdateBuilder<Thing>.Create().Set(x => x.Published, true).Set(x => x.Name, "a");

        CollectionAssert.AreEquivalent(new[] { "Published", "Name" },
            AuditFilterId.MemberPaths(builder).ToArray());
    }

    public class Thing : BaseEntity
    {
        public string Name { get; set; }
        public bool Published { get; set; }
        public InnerThing Inner { get; set; }
    }

    public class InnerThing
    {
        public int Value { get; set; }
    }
}
