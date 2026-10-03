using Grand.Data;
using Grand.Data.Tests.LiteDb;
using Grand.Domain.Admin;
using Grand.Module.Migration.Migrations._2._4;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Services.Migrations;

[TestClass]
public class MigrationRemoveAdminSiteMapHelpTests
{
    private LiteDBRepositoryMock<AdminSiteMap> _repository;
    private IServiceProvider _serviceProvider;
    private MigrationRemoveAdminSiteMapHelp _migration;

    [TestInitialize]
    public void Init()
    {
        _repository = new LiteDBRepositoryMock<AdminSiteMap>();

        var services = new ServiceCollection();
        services.AddSingleton<IRepository<AdminSiteMap>>(_repository);
        services.AddSingleton<ILogger<MigrationRemoveAdminSiteMapHelp>>(
            NullLogger<MigrationRemoveAdminSiteMapHelp>.Instance);
        _serviceProvider = services.BuildServiceProvider();

        _migration = new MigrationRemoveAdminSiteMapHelp();
    }

    private static AdminSiteMap SupportServices() => new() {
        SystemName = "Premium support services",
        ResourceName = "Admin.Help.SupportServices",
        Url = "https://grandnode.com/premium-support-packages"
    };

    [TestMethod]
    public void UpgradeProcess_StockHelpNode_RemovesTheWholeNode()
    {
        _repository.Insert(new AdminSiteMap { SystemName = "Plugins" });
        _repository.Insert(new AdminSiteMap { SystemName = "Help", ChildNodes = [SupportServices()] });

        Assert.IsTrue(_migration.UpgradeProcess(_serviceProvider));

        Assert.IsNull(_repository.Table.FirstOrDefault(x => x.SystemName == "Help"));
        Assert.AreEqual("Plugins", _repository.Table.Single().SystemName);
    }

    [TestMethod]
    public void UpgradeProcess_HelpWithOtherEntries_RemovesTheWholeNode()
    {
        _repository.Insert(new AdminSiteMap {
            SystemName = "Help",
            ChildNodes = [SupportServices(), new AdminSiteMap { SystemName = "Our wiki", Url = "https://wiki.example.com" }]
        });

        Assert.IsTrue(_migration.UpgradeProcess(_serviceProvider));

        Assert.AreEqual(0, _repository.Table.Count());
    }

    [TestMethod]
    public void UpgradeProcess_NoHelpNode_ChangesNothing()
    {
        _repository.Insert(new AdminSiteMap { SystemName = "Plugins" });

        Assert.IsTrue(_migration.UpgradeProcess(_serviceProvider));

        Assert.AreEqual("Plugins", _repository.Table.Single().SystemName);
    }

    [TestMethod]
    public void UpgradeProcess_RunTwice_IsIdempotent()
    {
        _repository.Insert(new AdminSiteMap { SystemName = "Help", ChildNodes = [SupportServices()] });

        Assert.IsTrue(_migration.UpgradeProcess(_serviceProvider));
        Assert.IsTrue(_migration.UpgradeProcess(_serviceProvider));

        Assert.AreEqual(0, _repository.Table.Count());
    }
}
