using Grand.Business.Catalog.Queries.Handlers;
using Grand.Business.Catalog.Startup;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Startup;

[TestClass]
public class ProductTextIndexHostedServiceTests
{
    private Mock<IDatabaseContext> _databaseContext;
    private Mock<ILogger<ProductTextIndexHostedService>> _logger;
    private IServiceScopeFactory _scopeFactory;

    [TestInitialize]
    public void Init()
    {
        _databaseContext = new Mock<IDatabaseContext>();
        _logger = new Mock<ILogger<ProductTextIndexHostedService>>();
        var services = new ServiceCollection();
        services.AddScoped(_ => _databaseContext.Object);
        services.AddScoped(_ => Mock.Of<IRepository<Product>>());
        _scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private Task Start(bool textSearch, bool liteDb = false, bool installed = true)
    {
        var service = new TestableService(_scopeFactory,
            new DatabaseConfig { UseProductTextSearch = textSearch, UseLiteDb = liteDb }, _logger.Object, installed);
        return service.StartingAsync(CancellationToken.None);
    }

    private void VerifyCreated(Times times)
    {
        _databaseContext.Verify(x => x.CreateTextIndex(It.IsAny<IRepository<Product>>(),
            ProductTextIndex.Fields, ProductTextIndex.Name), times);
    }

    [TestMethod]
    public async Task StartingAsync_SwitchOff_DoesNotCreateIndex()
    {
        await Start(false);

        VerifyCreated(Times.Never());
    }

    [TestMethod]
    public async Task StartingAsync_SwitchOn_CreatesIndex()
    {
        await Start(true);

        VerifyCreated(Times.Once());
    }

    [TestMethod]
    public async Task StartingAsync_LiteDb_DoesNotCreateIndex()
    {
        await Start(true, liteDb: true);

        VerifyCreated(Times.Never());
    }

    [TestMethod]
    public async Task StartingAsync_NotInstalled_DoesNotCreateIndex()
    {
        await Start(true, installed: false);

        VerifyCreated(Times.Never());
    }

    [TestMethod]
    public async Task StartingAsync_CreationFails_LogsErrorAndCompletes()
    {
        _databaseContext.Setup(x => x.CreateTextIndex(It.IsAny<IRepository<Product>>(), It.IsAny<string[]>(),
            It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("index"));

        await Start(true);

        _logger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<InvalidOperationException>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    private class TestableService(
        IServiceScopeFactory serviceScopeFactory,
        DatabaseConfig databaseConfig,
        ILogger<ProductTextIndexHostedService> logger,
        bool installed)
        : ProductTextIndexHostedService(serviceScopeFactory, databaseConfig, logger)
    {
        protected override bool DatabaseIsInstalled() => installed;
    }
}
