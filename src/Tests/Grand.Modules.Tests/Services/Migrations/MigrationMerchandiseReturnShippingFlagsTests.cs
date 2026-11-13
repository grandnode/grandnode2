using Grand.Data;
using Grand.Data.Tests.LiteDb;
using Grand.Domain.Orders;
using Grand.Module.Migration.Migrations._2._5;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Services.Migrations;

[TestClass]
public class MigrationMerchandiseReturnShippingFlagsTests
{
    private readonly MigrationMerchandiseReturnShippingFlags _migration = new();
    private readonly LiteDBRepositoryMock<MerchandiseReturnReason> _reasons = new();
    private IServiceProvider _serviceProvider;

    [TestInitialize]
    public void Init()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepository<MerchandiseReturnReason>>(_reasons);
        services.AddSingleton<ILogger<MigrationMerchandiseReturnShippingFlags>>(
            NullLogger<MigrationMerchandiseReturnShippingFlags>.Instance);
        _serviceProvider = services.BuildServiceProvider();
    }

    private bool Free(string id)
    {
        return _reasons.Table.First(x => x.Id == id).FreeReturnShipping;
    }

    private void SeedAll()
    {
        _reasons.Insert(new MerchandiseReturnReason { Id = "wrong", Name = "Received Wrong Product" });
        _reasons.Insert(new MerchandiseReturnReason { Id = "ordered", Name = "Wrong Product Ordered" });
        _reasons.Insert(new MerchandiseReturnReason { Id = "problem", Name = "There Was A Problem With The Product" });
    }

    [TestMethod]
    public async Task Upgrade_SeededNames_FlagsSet()
    {
        SeedAll();

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        Assert.IsTrue(Free("wrong"));
        Assert.IsTrue(Free("problem"));
        Assert.IsFalse(Free("ordered"));
    }

    [TestMethod]
    public async Task Upgrade_RenamedReason_Untouched()
    {
        _reasons.Insert(new MerchandiseReturnReason { Id = "renamed", Name = "Wrong item delivered" });
        _reasons.Insert(new MerchandiseReturnReason { Id = "case", Name = "received wrong product" });

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        Assert.IsFalse(Free("renamed"));
        Assert.IsFalse(Free("case"));
    }

    [TestMethod]
    public async Task Upgrade_RunTwice_SameResult()
    {
        SeedAll();

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));
        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        Assert.IsTrue(Free("wrong"));
        Assert.IsTrue(Free("problem"));
        Assert.IsFalse(Free("ordered"));
    }
}
