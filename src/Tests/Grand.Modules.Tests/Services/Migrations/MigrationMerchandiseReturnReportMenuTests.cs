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
public class MigrationMerchandiseReturnReportMenuTests
{
    private const string NodeName = "Merchandise returns report";
    private readonly MigrationMerchandiseReturnReportMenu _migration = new();
    private readonly LiteDBRepositoryMock<AdminSiteMap> _siteMap = new();
    private IServiceProvider _serviceProvider;

    public MigrationMerchandiseReturnReportMenuTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepository<AdminSiteMap>>(_siteMap);
        services.AddSingleton<ILogger<MigrationMerchandiseReturnReportMenu>>(
            NullLogger<MigrationMerchandiseReturnReportMenu>.Instance);
        _serviceProvider = services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task Adds_EntryUnderReports_Once()
    {
        _siteMap.Insert(new AdminSiteMap {
            SystemName = "Reports",
            ChildNodes = new List<AdminSiteMap> {
                new() { SystemName = "Low stock report", DisplayOrder = 0 },
                new() { SystemName = "Customer reports", DisplayOrder = 4 }
            }
        });

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));
        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        var nodes = _siteMap.Table.Single().ChildNodes;
        var node = nodes.Single(x => x.SystemName == NodeName);
        var installed = StandardAdminSiteMap.SiteMap.Single(x => x.SystemName == "Reports").ChildNodes
            .Single(x => x.SystemName == NodeName);
        Assert.AreEqual(installed.ResourceName, node.ResourceName);
        Assert.AreEqual("Reports", node.ControllerName);
        Assert.AreEqual("MerchandiseReturnReport", node.ActionName);
        CollectionAssert.AreEqual(installed.PermissionNames.ToArray(), node.PermissionNames.ToArray());
        Assert.AreEqual(NodeName, nodes.OrderBy(x => x.DisplayOrder).Last().SystemName);
    }

    [TestMethod]
    public async Task NoReportsNode_NothingAdded()
    {
        _siteMap.Insert(new AdminSiteMap { SystemName = "Configuration", ChildNodes = new List<AdminSiteMap>() });

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        Assert.IsEmpty(_siteMap.Table.Single().ChildNodes);
    }
}
