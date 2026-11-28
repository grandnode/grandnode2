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
public class MigrationRecurringPaymentMenuTests
{
    private const string NodeName = "Recurring payments";
    private readonly MigrationRecurringPaymentMenu _migration = new();
    private readonly LiteDBRepositoryMock<AdminSiteMap> _siteMap = new();
    private IServiceProvider _serviceProvider;

    public MigrationRecurringPaymentMenuTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepository<AdminSiteMap>>(_siteMap);
        services.AddSingleton<ILogger<MigrationRecurringPaymentMenu>>(
            NullLogger<MigrationRecurringPaymentMenu>.Instance);
        _serviceProvider = services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task Adds_EntryUnderSalesAfterPaymentTransactions_Once()
    {
        _siteMap.Insert(new AdminSiteMap {
            SystemName = "Sales",
            ChildNodes = new List<AdminSiteMap> {
                new() { SystemName = "Orders", DisplayOrder = 0 },
                new() { SystemName = "Payment transaction", DisplayOrder = 3 },
                new() { SystemName = "OrderTags", DisplayOrder = 4 },
                new() { SystemName = "Order status", DisplayOrder = 6 }
            }
        });

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));
        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        var nodes = _siteMap.Table.Single().ChildNodes;
        var node = nodes.Single(x => x.SystemName == NodeName);
        var installed = StandardAdminSiteMap.SiteMap.Single(x => x.SystemName == "Sales").ChildNodes
            .Single(x => x.SystemName == NodeName);
        Assert.AreEqual(installed.ResourceName, node.ResourceName);
        Assert.AreEqual(installed.ControllerName, node.ControllerName);
        Assert.AreEqual(installed.ActionName, node.ActionName);
        CollectionAssert.AreEqual(installed.PermissionNames.ToArray(), node.PermissionNames.ToArray());
        CollectionAssert.AreEqual(new[] { "Orders", "Payment transaction", NodeName, "OrderTags", "Order status" },
            nodes.OrderBy(x => x.DisplayOrder).Select(x => x.SystemName).ToArray());
        Assert.AreEqual(4, node.DisplayOrder);
        Assert.AreEqual(5, nodes.Single(x => x.SystemName == "OrderTags").DisplayOrder);
    }

    [TestMethod]
    public async Task NoSalesNode_NothingAdded()
    {
        _siteMap.Insert(new AdminSiteMap { SystemName = "Configuration", ChildNodes = new List<AdminSiteMap>() });

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        Assert.IsEmpty(_siteMap.Table.Single().ChildNodes);
    }
}
