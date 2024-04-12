using Grand.Infrastructure.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.Migrations;

[TestClass]
public class DbVersionTests
{
    [TestMethod]
    public void CompareToTest_Greater_Major()
    {
        var dbVersion1 = new DbVersion(1, 0);
        var dbVersion2 = new DbVersion(2, 0);
        var result = dbVersion2.CompareTo(dbVersion1);
        Assert.AreEqual(result, 1);
    }

    [TestMethod]
    public void CompareToTest_Lower_Major()
    {
        var dbVersion1 = new DbVersion(1, 0);
        var dbVersion2 = new DbVersion(2, 0);
        var result = dbVersion1.CompareTo(dbVersion2);
        Assert.AreEqual(result, -1);
    }

    [TestMethod]
    public void CompareToTest_Greater_Minor()
    {
        var dbVersion1 = new DbVersion(2, 0);
        var dbVersion2 = new DbVersion(2, 1);
        var result = dbVersion2.CompareTo(dbVersion1);
        Assert.AreEqual(result, 1);
    }

    [TestMethod]
    public void CompareToTest_Lower_Minor()
    {
        var dbVersion1 = new DbVersion(2, 0);
        var dbVersion2 = new DbVersion(2, 1);
        var result = dbVersion1.CompareTo(dbVersion2);
        Assert.AreEqual(result, -1);
    }

    [TestMethod]
    public void CompareToTest_Eq()
    {
        var dbVersion1 = new DbVersion(2, 0);
        var dbVersion2 = new DbVersion(2, 0);
        var result = dbVersion1.CompareTo(dbVersion2);
        Assert.AreEqual(result, 0);
    }
}