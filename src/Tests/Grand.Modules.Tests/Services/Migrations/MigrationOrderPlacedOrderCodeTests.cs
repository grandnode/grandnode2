using Grand.Data;
using Grand.Data.Tests.LiteDb;
using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Module.Installer.Services.MessageTemplates;
using Grand.Module.Migration.Migrations._2._5;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Services.Migrations;

/// <summary>
///     The order confirmation of an existing installation gets the order code row the installer seeds, unless the
///     operator rewrote its order number row
/// </summary>
[TestClass]
public class MigrationOrderPlacedOrderCodeTests
{
    private const string TemplateName = "OrderPlaced.CustomerNotification";

    private Mock<ICacheBase> _cache;
    private MigrationOrderPlacedOrderCode _migration;
    private IServiceProvider _serviceProvider;
    private LiteDBRepositoryMock<MessageTemplate> _templates;

    [TestInitialize]
    public void Init()
    {
        _templates = new LiteDBRepositoryMock<MessageTemplate>();
        _cache = new Mock<ICacheBase>();

        var services = new ServiceCollection();
        services.AddSingleton<IRepository<MessageTemplate>>(_templates);
        services.AddSingleton(_cache.Object);
        services.AddSingleton<ILogger<MigrationOrderPlacedOrderCode>>(
            NullLogger<MigrationOrderPlacedOrderCode>.Instance);
        _serviceProvider = services.BuildServiceProvider();
        _migration = new MigrationOrderPlacedOrderCode();
    }

    /// <summary>
    ///     The body an installation of 2.4 was seeded with: today's seed without the code row
    /// </summary>
    private static string SeededBefore()
    {
        var seeded = MessageTemplateSeed.Build("ea").Single(x => x.Name == TemplateName).Body;
        var index = seeded.IndexOf(MigrationOrderPlacedOrderCode.OrderCodeRow, StringComparison.Ordinal);
        return seeded.Remove(index, MigrationOrderPlacedOrderCode.OrderCodeRow.Length);
    }

    [TestMethod]
    public void Seed_HoldsBothRows()
    {
        var seeded = MessageTemplateSeed.Build("ea").Single(x => x.Name == TemplateName).Body;

        StringAssert.Contains(seeded, MigrationOrderPlacedOrderCode.OrderNumberRow);
        StringAssert.Contains(seeded, MigrationOrderPlacedOrderCode.OrderCodeRow);
    }

    [TestMethod]
    public async Task Upgrade_SeededBody_GetsCodeRowOnce()
    {
        _templates.Insert(new MessageTemplate { Name = TemplateName, Body = SeededBefore() });

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));
        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        var body = _templates.Table.Single().Body;
        var at = body.IndexOf(MigrationOrderPlacedOrderCode.OrderCodeRow, StringComparison.Ordinal);
        Assert.IsTrue(at > body.IndexOf(MigrationOrderPlacedOrderCode.OrderNumberRow, StringComparison.Ordinal));
        Assert.AreEqual(at, body.LastIndexOf(MigrationOrderPlacedOrderCode.OrderCodeRow, StringComparison.Ordinal));
        _cache.Verify(x => x.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY, It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public async Task Upgrade_RewrittenBody_Untouched()
    {
        _templates.Insert(new MessageTemplate { Name = TemplateName, Body = "<p>Order {{Order.OrderNumber}}</p>" });
        _templates.Insert(new MessageTemplate { Name = "OrderPaid.CustomerNotification", Body = SeededBefore() });

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        Assert.AreEqual("<p>Order {{Order.OrderNumber}}</p>", _templates.Table.Single(x => x.Name == TemplateName).Body);
        Assert.AreEqual(SeededBefore(), _templates.Table.Single(x => x.Name != TemplateName).Body);
        _cache.Verify(x => x.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task Upgrade_RepositoryFails_ReturnsFalseWithoutThrowing()
    {
        var failing = new Mock<IRepository<MessageTemplate>>();
        failing.SetupGet(x => x.Table).Throws(new InvalidOperationException("db"));
        var services = new ServiceCollection();
        services.AddSingleton(failing.Object);
        services.AddSingleton<ILogger<MigrationOrderPlacedOrderCode>>(
            NullLogger<MigrationOrderPlacedOrderCode>.Instance);

        Assert.IsFalse(await _migration.UpgradeProcess(services.BuildServiceProvider()));
    }
}
