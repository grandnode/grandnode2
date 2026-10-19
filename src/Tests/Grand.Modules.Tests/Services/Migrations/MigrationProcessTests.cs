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
    public async Task RunProcess_MigrationThrows_ReportsFailureWithoutRecording()
    {
        var migration = Migration(true);
        migration.Setup(x => x.UpgradeProcess(It.IsAny<IServiceProvider>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        var result = await _migrationProcess.RunProcess(migration.Object);

        Assert.IsFalse(result.Success);
        Assert.IsFalse(_migrationRepository.Table.Any());
    }

    [TestMethod]
    public async Task RunMigrationProcess_NoVersionRecord_RunsNothing()
    {
        await _migrationProcess.RunMigrationProcess();

        Assert.IsFalse(_migrationRepository.Table.Any());
    }

    [TestMethod]
    public async Task RunMigrationProcess_FailedMigration_StopsTheRun()
    {
        await _versionRepository.InsertAsync(new GrandNodeVersion { InstalledVersion = "2.4" });
        var pending = new[] { Migration(true).Object, Migration(true).Object, Migration(true).Object };

        var process = new RecordingMigrationProcess(_serviceProvider, _migrationRepository, _versionRepository,
            pending, failing: pending[1]);
        await process.RunMigrationProcess();

        CollectionAssert.AreEqual(new[] { pending[0].Identity, pending[1].Identity }, process.Ran);
    }

    [TestMethod]
    public async Task RunMigrationProcess_SkipsRecordedMigrations()
    {
        await _versionRepository.InsertAsync(new GrandNodeVersion { InstalledVersion = "2.4" });
        var pending = new[] { Migration(true).Object, Migration(true).Object };
        await _migrationRepository.InsertAsync(new MigrationDb { Identity = pending[0].Identity });

        var process = new RecordingMigrationProcess(_serviceProvider, _migrationRepository, _versionRepository,
            pending, failing: null);
        await process.RunMigrationProcess();

        CollectionAssert.AreEqual(new[] { pending[1].Identity }, process.Ran);
    }

    private class RecordingMigrationProcess(
        IServiceProvider serviceProvider,
        IRepository<MigrationDb> repositoryMigration,
        IRepository<GrandNodeVersion> repositoryVersion,
        IList<IMigration> pending,
        IMigration failing)
        : MigrationProcess(Mock.Of<IDatabaseContext>(), serviceProvider, NullLogger<MigrationProcess>.Instance,
            repositoryMigration, repositoryVersion)
    {
        public List<Guid> Ran { get; } = [];

        //MigrationManager scans every loaded assembly, Moq proxies of IMigration included
        protected override IEnumerable<IMigration> GetCurrentMigrations(DbVersion installedVersion) => pending;

        public override Task<MigrationResult> RunProcess(IMigration migration)
        {
            Ran.Add(migration.Identity);
            return Task.FromResult(new MigrationResult { Migration = migration, Success = migration != failing });
        }
    }
}
