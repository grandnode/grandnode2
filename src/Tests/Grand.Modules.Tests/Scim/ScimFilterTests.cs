using Grand.Module.Scim.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Scim;

[TestClass]
public class ScimFilterTests
{
    [TestMethod]
    [DataRow("userName eq \"jan@example.com\"", "username", "jan@example.com")]
    [DataRow("USERNAME EQ \"jan@example.com\"", "username", "jan@example.com")]
    [DataRow("  externalId eq \"a-b-c\"  ", "externalid", "a-b-c")]
    [DataRow("displayName eq \"Shop \\\"admins\\\"\"", "displayname", "Shop \"admins\"")]
    [DataRow("userName eq \"\"", "username", "")]
    public void Parses(string filter, string attribute, string value)
    {
        Assert.IsTrue(ScimFilter.TryParse(filter, out var parsed));
        Assert.AreEqual(attribute, parsed.Attribute);
        Assert.AreEqual(value, parsed.Value);
    }

    [TestMethod]
    [DataRow("userName co \"jan\"")]
    [DataRow("userName eq jan")]
    [DataRow("userName eq \"a\" and active eq true")]
    [DataRow("userName pr")]
    [DataRow("")]
    [DataRow(null)]
    public void RefusesAnythingElse(string filter) =>
        Assert.IsFalse(ScimFilter.TryParse(filter, out _));
}
