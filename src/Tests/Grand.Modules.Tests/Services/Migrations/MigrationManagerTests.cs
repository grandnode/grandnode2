using Grand.Infrastructure.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Services.Migrations;

[TestClass]
public class MigrationManagerTests
{
    private MigrationManager _migrationManager;

    [TestInitialize]
    public void Init()
    {
        _migrationManager = new MigrationManager();
    }

    [TestMethod]
    public void GetCurrentMigrations_Exists()
    {
        var migrations = _migrationManager.GetCurrentMigrations(new DbVersion(2, 2));
        Assert.IsTrue(migrations.Count() > 0);
    }
}