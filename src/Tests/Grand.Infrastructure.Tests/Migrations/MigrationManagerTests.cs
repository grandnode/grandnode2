using Grand.Infrastructure.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.Migrations;

[TestClass]
public class MigrationManagerTests
{
    private readonly MigrationManager migrationManager = new();

    [TestMethod]
    public void GetAllMigrationsTest()
    {
        var result = migrationManager.GetAllMigrations();
        Assert.AreEqual(2, result.Count());
    }

    [TestMethod]
    public void GetCurrentMigrationsTest()
    {
        var result = migrationManager.GetCurrentMigrations(new DbVersion(2,3));
        Assert.AreEqual(1, result.Count());
    }
}