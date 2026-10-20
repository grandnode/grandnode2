using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Configuration;
using Grand.Module.Migration.Migrations._2._5;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Services.Migrations;

[TestClass]
public class MigrationProductTextIndexTests
{
    private Mock<IDatabaseContext> _context;

    [TestInitialize]
    public void Setup()
    {
        _context = new Mock<IDatabaseContext>();
    }

    private Task<bool> Upgrade(bool textSearch)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new DatabaseConfig { UseProductTextSearch = textSearch });
        services.AddSingleton(Mock.Of<IRepository<Product>>());
        services.AddSingleton(_context.Object);
        services.AddSingleton<ILogger<MigrationProductTextIndex>>(NullLogger<MigrationProductTextIndex>.Instance);
        return new MigrationProductTextIndex().UpgradeProcess(services.BuildServiceProvider());
    }

    private void VerifyCreated(Times times)
    {
        _context.Verify(x => x.CreateTextIndex(It.IsAny<IRepository<Product>>(), ProductTextIndex.Fields,
            ProductTextIndex.Name, It.IsAny<CancellationToken>()), times);
    }

    [TestMethod]
    public async Task SwitchOff_DoesNotCreateIndex()
    {
        Assert.IsTrue(await Upgrade(false));

        VerifyCreated(Times.Never());
    }

    [TestMethod]
    public async Task SwitchOn_CreatesIndex()
    {
        Assert.IsTrue(await Upgrade(true));

        VerifyCreated(Times.Once());
    }

    [TestMethod]
    public async Task CreationFails_FailsTheMigration()
    {
        _context.Setup(x => x.CreateTextIndex(It.IsAny<IRepository<Product>>(), It.IsAny<string[]>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("index"));

        Assert.IsFalse(await Upgrade(true));
    }
}
