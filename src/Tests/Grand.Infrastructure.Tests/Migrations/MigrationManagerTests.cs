using Grand.Infrastructure.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.Migrations
{
    [TestClass()]
    public class MigrationManagerTests
    {
        MigrationManager migrationManager;

        public MigrationManagerTests()
        {
            migrationManager = new MigrationManager();
        }

        [TestMethod()]
        public void GetAllMigrationsTest()
        {
            var result = migrationManager.GetAllMigrations();
            Assert.IsTrue(result.Count() == 2);
        }

        [TestMethod()]
        public void GetCurrentMigrationsTest()
        {
            var result = migrationManager.GetCurrentMigrations();
            Assert.IsTrue(result.Count() == 1);
        }
    }
}