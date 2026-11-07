using Grand.Business.Core.Utilities.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Utilities;

[TestClass]
public class UnitNumbersTests
{
    [TestMethod]
    public void Key_TrimsAndUppercases()
    {
        Assert.AreEqual("SN-01", UnitNumbers.Key(" sn-01 "));
    }

    [TestMethod]
    public void Key_Null_Empty()
    {
        Assert.AreEqual("", UnitNumbers.Key(null));
    }

    [TestMethod]
    public void Parse_SplitsLinesCommasSemicolons_DropsEmpty()
    {
        CollectionAssert.AreEqual(new[] { "a1", "B2", "c3" }, UnitNumbers.Parse("a1\r\n\n B2 ;c3,").ToArray());
    }

    [TestMethod]
    public void Parse_Null_Empty()
    {
        Assert.AreEqual(0, UnitNumbers.Parse(null).Count);
    }

    [TestMethod]
    public void Duplicates_ByKey()
    {
        CollectionAssert.AreEqual(new[] { "SN1" }, UnitNumbers.Duplicates(["SN1", "sn1", "x"]).ToArray());
    }
}
