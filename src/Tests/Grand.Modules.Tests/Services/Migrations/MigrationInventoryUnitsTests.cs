using Grand.Data;
using Grand.Data.Tests.LiteDb;
using Grand.Domain.Admin;
using Grand.Domain.Catalog;
using Grand.Domain.Configuration;
using Grand.Domain.Messages;
using Grand.Domain.Tasks;
using Grand.Infrastructure.Caching;
using Grand.Module.Installer.Services.MessageTemplates;
using Grand.Module.Migration.Migrations._2._5;
using Grand.Modules.Tests.Installer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Text.RegularExpressions;

namespace Grand.Modules.Tests.Services.Migrations;

[TestClass]
public class MigrationInventoryUnitsTests
{
    private const string TemplateName = "Inventory.BatchesExpiring.StoreOwnerNotification";
    private readonly MigrationInventoryUnits _migration = new();
    private readonly LiteDBRepositoryMock<AdminSiteMap> _siteMap = new();
    private readonly LiteDBRepositoryMock<ScheduleTask> _tasks = new();
    private readonly LiteDBRepositoryMock<MessageTemplate> _templates = new();
    private IServiceProvider _serviceProvider;

    [TestInitialize]
    public void Init()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepository<MessageTemplate>>(_templates);
        services.AddSingleton<IRepository<EmailAccount>>(new LiteDBRepositoryMock<EmailAccount>());
        services.AddSingleton<IRepository<Setting>>(new LiteDBRepositoryMock<Setting>());
        services.AddSingleton<IRepository<ScheduleTask>>(_tasks);
        services.AddSingleton<IRepository<AdminSiteMap>>(_siteMap);
        services.AddSingleton<IRepository<ProductBatch>>(new LiteDBRepositoryMock<ProductBatch>());
        services.AddSingleton<IRepository<ProductSerialNumber>>(new LiteDBRepositoryMock<ProductSerialNumber>());
        services.AddSingleton<IRepository<InventoryJournal>>(new LiteDBRepositoryMock<InventoryJournal>());
        services.AddSingleton(new Mock<IDatabaseContext>().Object);
        services.AddSingleton(new Mock<ICacheBase>().Object);
        services.AddSingleton<ILogger<MigrationInventoryUnits>>(NullLogger<MigrationInventoryUnits>.Instance);
        _serviceProvider = services.BuildServiceProvider();
        _siteMap.Insert(new AdminSiteMap { SystemName = "Catalog", ChildNodes = new List<AdminSiteMap>() });
        _siteMap.Insert(new AdminSiteMap {
            SystemName = "Configuration",
            ChildNodes = new List<AdminSiteMap> {
                new() { SystemName = "Shipping", ChildNodes = new List<AdminSiteMap> { new() { SystemName = "Warehouses" } } }
            }
        });
    }

    [TestMethod]
    public async Task Upgrade_RunTwice_OneTaskOneTemplateInventoryNodesOnce()
    {
        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));
        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        Assert.AreEqual(1, _tasks.Table.Count(x => x.ScheduleTaskName == "Inventory batch expiry notification"));
        Assert.AreEqual(1, _templates.Table.Count(x => x.Name == TemplateName));
        Assert.AreEqual(0, _siteMap.Table.Single(x => x.SystemName == "Catalog").ChildNodes.Count);
        var inventory = _siteMap.Table.Single(x => x.SystemName == "Inventory");
        CollectionAssert.AreEquivalent(new[] { "Batches", "Serial numbers" },
            inventory.ChildNodes.Select(x => x.SystemName).ToArray());
        var shipping = _siteMap.Table.Single(x => x.SystemName == "Configuration").ChildNodes.Single();
        CollectionAssert.AreEqual(new[] { "Warehouses", "Warehouse locations" },
            shipping.ChildNodes.Select(x => x.SystemName).ToArray());
    }

    [TestMethod]
    public async Task Upgrade_InventorySectionExists_NodesAddedToIt()
    {
        _siteMap.Insert(new AdminSiteMap {
            SystemName = "Inventory",
            ChildNodes = new List<AdminSiteMap> { new() { SystemName = "Inventory documents" } }
        });

        await _migration.UpgradeProcess(_serviceProvider);

        var inventory = _siteMap.Table.Single(x => x.SystemName == "Inventory");
        CollectionAssert.AreEquivalent(new[] { "Inventory documents", "Batches", "Serial numbers" },
            inventory.ChildNodes.Select(x => x.SystemName).ToArray());
    }

    [TestMethod]
    public async Task Upgrade_ExistingTemplate_Untouched()
    {
        _templates.Insert(new MessageTemplate { Name = TemplateName, Subject = "Our subject" });

        await _migration.UpgradeProcess(_serviceProvider);

        Assert.AreEqual("Our subject", _templates.Table.Single(x => x.Name == TemplateName).Subject);
    }

    [TestMethod]
    public void Template_EqualsInstallerTemplate()
    {
        var seeded = MessageTemplateSeed.Build("ea").Single(x => x.Name == TemplateName);
        var migrated = MigrationInventoryUnits.Template("ea");

        Assert.AreEqual(seeded.Subject, migrated.Subject);
        CollectionAssert.AreEqual(MessageTemplateSeedTests.Tokens(seeded.Body).ToList(),
            MessageTemplateSeedTests.Tokens(migrated.Body).ToList());
        Assert.AreEqual(Regex.Replace(seeded.Body, @"\s+", " "), Regex.Replace(migrated.Body, @"\s+", " "));
    }
}
