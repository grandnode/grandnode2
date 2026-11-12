using Grand.Data;
using Grand.Data.Tests.LiteDb;
using Grand.Domain.Admin;
using Grand.Module.Installer.Utilities;
using Grand.Module.Migration.Migrations._2._5;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Services.Migrations;

[TestClass]
public class MigrationReturnShippingMenuTests
{
    private const string NodeName = "Return methods";
    private readonly MigrationReturnShippingMenu _migration = new();
    private readonly LiteDBRepositoryMock<AdminSiteMap> _siteMap = new();
    private IServiceProvider _serviceProvider;

    [TestInitialize]
    public void Init()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepository<AdminSiteMap>>(_siteMap);
        services.AddSingleton<ILogger<MigrationReturnShippingMenu>>(NullLogger<MigrationReturnShippingMenu>.Instance);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void SeedShipping()
    {
        _siteMap.Insert(new AdminSiteMap {
            SystemName = "Configuration",
            ChildNodes = new List<AdminSiteMap> {
                new() {
                    SystemName = "Shipping",
                    ChildNodes = new List<AdminSiteMap> {
                        new() { SystemName = "Shipping providers", DisplayOrder = 0 },
                        new() { SystemName = "Shipping methods", DisplayOrder = 1 }
                    }
                }
            }
        });
    }

    private static AdminSiteMap Shipping(IEnumerable<AdminSiteMap> roots)
    {
        return roots.Single(x => x.SystemName == "Configuration").ChildNodes.Single(x => x.SystemName == "Shipping");
    }

    [TestMethod]
    public async Task Menu_Upgrade_AddsNodeOnce()
    {
        SeedShipping();

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));
        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        var nodes = Shipping(_siteMap.Table).ChildNodes;
        var node = nodes.Single(x => x.SystemName == NodeName);
        Assert.AreEqual("Admin.Configuration.Shipping.ReturnMethods", node.ResourceName);
        Assert.AreEqual("ReturnShipping", node.ControllerName);
        Assert.AreEqual("Providers", node.ActionName);
        //right after the shipping providers, the menu orders by display order
        CollectionAssert.AreEqual(new[] { "Shipping providers", NodeName, "Shipping methods" },
            nodes.OrderBy(x => x.DisplayOrder).Select(x => x.SystemName).ToArray());
    }

    [TestMethod]
    public async Task Menu_Upgrade_NoShippingNode_NothingAdded()
    {
        _siteMap.Insert(new AdminSiteMap { SystemName = "Configuration", ChildNodes = new List<AdminSiteMap>() });

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        Assert.IsEmpty(_siteMap.Table.Single().ChildNodes);
    }

    [TestMethod]
    public async Task Menu_Upgrade_NodeEqualsInstallerNode()
    {
        SeedShipping();

        await _migration.UpgradeProcess(_serviceProvider);

        var migrated = Shipping(_siteMap.Table).ChildNodes.Single(x => x.SystemName == NodeName);
        var installed = Shipping(StandardAdminSiteMap.SiteMap).ChildNodes.Single(x => x.SystemName == NodeName);
        Assert.AreEqual(installed.ResourceName, migrated.ResourceName);
        Assert.AreEqual(installed.ControllerName, migrated.ControllerName);
        Assert.AreEqual(installed.ActionName, migrated.ActionName);
        Assert.AreEqual(installed.DisplayOrder, migrated.DisplayOrder);
        Assert.AreEqual(installed.IconClass, migrated.IconClass);
    }
}
