using Grand.Data;
using Grand.Domain.Common;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Services.Migrations;

[TestClass]
public class MigrationManagerTests
{
    /// <summary>
    ///     MigrationManager scans loaded assemblies, and a project reference alone does not load one.
    ///     Holding the assembly in a field keeps it loaded before the first scan.
    /// </summary>
    private static readonly System.Reflection.Assembly MigrationsAssembly =
        typeof(Grand.Module.Migration.Migrations.MigrationUpgradeDbVersionBase).Assembly;

    private MigrationManager _migrationManager;

    /// <summary>
    ///     Every migration that ships, from the oldest supported database version upwards.
    /// </summary>
    private IList<IMigration> AllShippedMigrations =>
        _migrationManager.GetCurrentMigrations(new DbVersion(1, 0))
            //test fixtures in this assembly also implement IMigration - keep them out
            .Where(x => x.GetType().Assembly == MigrationsAssembly)
            .ToList();

    [TestInitialize]
    public void Init()
    {
        Assert.IsNotNull(MigrationsAssembly);
        _migrationManager = new MigrationManager();
    }

    [TestMethod]
    public void GetCurrentMigrations_Exists()
    {
        var migrations = _migrationManager.GetCurrentMigrations(new DbVersion(2, 2));
        Assert.IsNotEmpty(migrations);
    }

    [TestMethod]
    public void GetCurrentMigrations_OrderedByAscendingVersion()
    {
        var migrations = AllShippedMigrations;

        for (var i = 1; i < migrations.Count; i++)
            Assert.IsTrue(migrations[i].Version.CompareTo(migrations[i - 1].Version) >= 0,
                $"{migrations[i].Name} ({migrations[i].Version}) runs after {migrations[i - 1].Name} ({migrations[i - 1].Version})");
    }

    [TestMethod]
    public void GetCurrentMigrations_VersionStampIsLastWithinItsVersion()
    {
        var migrations = AllShippedMigrations;

        foreach (var group in migrations.GroupBy(x => x.Version.ToString()))
        {
            var stamp = group.OfType<IMigrationVersionStamp>().SingleOrDefault();
            if (stamp == null) continue;

            Assert.AreSame(stamp, group.Last(),
                $"The version stamp for {group.Key} must run last, otherwise the remaining migrations of that version are skipped permanently");
        }
    }

    [TestMethod]
    public void AllMigrations_HaveUniqueIdentity()
    {
        var duplicates = AllShippedMigrations
            .GroupBy(x => x.Identity)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        Assert.IsEmpty(duplicates, $"Duplicated migration identity: {string.Join(", ", duplicates)}");
    }

    [TestMethod]
    public void VersionStamp_RecordsItsOwnVersion()
    {
        var stamps = AllShippedMigrations.OfType<IMigrationVersionStamp>().ToList();
        Assert.IsNotEmpty(stamps);

        foreach (var stamp in stamps)
        {
            var dbVersion = new GrandNodeVersion { DataBaseVersion = "1.0", InstalledVersion = "1.0" };

            var repository = new Mock<IRepository<GrandNodeVersion>>();
            repository.Setup(x => x.Table).Returns(new List<GrandNodeVersion> { dbVersion }.AsQueryable());

            var services = new ServiceCollection();
            services.AddSingleton(repository.Object);

            Assert.IsTrue(stamp.UpgradeProcess(services.BuildServiceProvider()), $"{stamp.Name} failed");

            Assert.AreEqual(stamp.Version.ToString(), dbVersion.DataBaseVersion,
                $"{stamp.Name} must record its own version, not the version the build supports");
            Assert.AreEqual(stamp.Version.ToString(), dbVersion.InstalledVersion,
                $"{stamp.Name} must record its own version in InstalledVersion too - the migration process reads it first");
        }
    }
}
