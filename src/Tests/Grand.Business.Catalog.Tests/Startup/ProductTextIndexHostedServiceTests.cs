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

    private Task Run(bool textSearch, bool installed = true, CancellationToken stoppingToken = default)
    {
        var service = new TestableService(_scopeFactory, new DatabaseConfig { UseProductTextSearch = textSearch },
            _logger.Object, installed);
        return service.Run(stoppingToken);
    }

    private void VerifyCreated(Times times)
    {
        _databaseContext.Verify(x => x.CreateTextIndex(It.IsAny<IRepository<Product>>(),
            ProductTextIndex.Fields, ProductTextIndex.Name, It.IsAny<CancellationToken>()), times);
    }

    private void VerifyErrorLogged(Times times)
    {
        _logger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), times);
    }

    [TestMethod]
    public async Task Execute_SwitchOff_DoesNotCreateIndex()
    {
        await Run(false);

        VerifyCreated(Times.Never());
    }

    [TestMethod]
    public async Task Execute_SwitchOn_CreatesIndex()
    {
        await Run(true);

        VerifyCreated(Times.Once());
    }

    [TestMethod]
    public async Task Execute_NotInstalled_DoesNotCreateIndex()
    {
        await Run(true, installed: false);

        VerifyCreated(Times.Never());
    }

    [TestMethod]
    public async Task Execute_CreationFails_LogsErrorAndCompletes()
    {
        _databaseContext.Setup(x => x.CreateTextIndex(It.IsAny<IRepository<Product>>(), It.IsAny<string[]>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("index"));

        await Run(true);

        VerifyErrorLogged(Times.Once());
    }

    [TestMethod]
    public async Task Execute_HostStopping_PassesTokenAndLogsNothing()
    {
        using var stopping = new CancellationTokenSource();
        await stopping.CancelAsync();
        _databaseContext.Setup(x => x.CreateTextIndex(It.IsAny<IRepository<Product>>(), It.IsAny<string[]>(),
                It.IsAny<string>(), stopping.Token))
            .ThrowsAsync(new OperationCanceledException(stopping.Token));

        await Run(true, stoppingToken: stopping.Token);

        VerifyErrorLogged(Times.Never());
    }

    private class TestableService(
        IServiceScopeFactory serviceScopeFactory,
        DatabaseConfig databaseConfig,
        ILogger<ProductTextIndexHostedService> logger,
        bool installed)
        : ProductTextIndexHostedService(serviceScopeFactory, databaseConfig, logger)
    {
        protected override bool DatabaseIsInstalled() => installed;

        public Task Run(CancellationToken stoppingToken) => ExecuteAsync(stoppingToken);
    }
}
