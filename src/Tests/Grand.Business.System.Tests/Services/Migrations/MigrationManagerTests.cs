using Grand.Infrastructure.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.System.Tests.Services.Migrations
{
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
            var migrations = _migrationManager.GetCurrentMigrations();
            Assert.IsTrue(migrations.Count() > 0);
        }

    }
}
