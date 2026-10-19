using Grand.Data;
using Grand.Data.Tests.LiteDb;
using Grand.Domain;
using Grand.Domain.Common;
using Grand.Infrastructure.Migrations;
using Grand.Module.Migration.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Services.Migrations;

[TestClass]
public class MigrationProcessTests
{
    private LiteDBRepositoryMock<MigrationDb> _migrationRepository;
    private LiteDBRepositoryMock<GrandNodeVersion> _versionRepository;
    private IServiceProvider _serviceProvider;
    private MigrationProcess _migrationProcess;

    [TestInitialize]
    public void Init()
    {
        _migrationRepository = new LiteDBRepositoryMock<MigrationDb>();
        _versionRepository = new LiteDBRepositoryMock<GrandNodeVersion>();
        _serviceProvider = new ServiceCollection().BuildServiceProvider();

        _migrationProcess = new MigrationProcess(Mock.Of<IDatabaseContext>(), _serviceProvider,
            NullLogger<MigrationProcess>.Instance, _migrationRepository, _versionRepository);
    }

    private static Mock<IMigration> Migration(bool success)
    {
        var migration = new Mock<IMigration>();
        migration.Setup(x => x.Identity).Returns(Guid.NewGuid());
        migration.Setup(x => x.Name).Returns("Test migration");
        migration.Setup(x => x.Version).Returns(new DbVersion(2, 5));
        migration.Setup(x => x.UpgradeProcess(It.IsAny<IServiceProvider>())).ReturnsAsync(success);
        return migration;
    }

    [TestMethod]
    public async Task RunProcess_Success_RecordsTheMigration()
    {
        var migration = Migration(true);

        var result = await _migrationProcess.RunProcess(migration.Object);

        Assert.IsTrue(result.Success);
        var recorded = _migrationRepository.Table.Single();
        Assert.AreEqual(migration.Object.Identity, recorded.Identity);
        Assert.AreEqual("2.5", recorded.Version);
        migration.Verify(x => x.UpgradeProcess(_serviceProvider), Times.Once);
    }

    [TestMethod]
    public async Task RunProcess_Failure_DoesNotRecordTheMigration()
    {
        var result = await _migrationProcess.RunProcess(Migration(false).Object);

        Assert.IsFalse(result.Success);
        Assert.IsFalse(_migrationRepository.Table.Any());
    }

    [TestMethod]
    public async Task RunMigrationProcess_NoVersionRecord_RunsNothing()
    {
        await _migrationProcess.RunMigrationProcess();

        Assert.IsFalse(_migrationRepository.Table.Any());
    }
}
