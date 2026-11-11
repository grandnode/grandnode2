using Grand.Data;
using Grand.Data.Tests.LiteDb;
using Grand.Domain.Orders;
using Grand.Module.Migration.Migrations._2._5;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Services.Migrations;

[TestClass]
public class MigrationMerchandiseReturnOrderIndexTests
{
    private readonly Mock<IDatabaseContext> _context = new();
    private readonly LiteDBRepositoryMock<MerchandiseReturn> _repository = new();
    private IServiceProvider _serviceProvider;

    [TestInitialize]
    public void Init()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepository<MerchandiseReturn>>(_repository);
        services.AddSingleton(_context.Object);
        services.AddSingleton<ILogger<MigrationMerchandiseReturnOrderIndex>>(
            NullLogger<MigrationMerchandiseReturnOrderIndex>.Instance);
        _serviceProvider = services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task UpgradeProcess_CreatesOrderIdIndex()
    {
        Assert.IsTrue(await new MigrationMerchandiseReturnOrderIndex().UpgradeProcess(_serviceProvider));

        _context.Verify(x => x.CreateIndex(_repository, It.IsAny<OrderBuilder<MerchandiseReturn>>(), "OrderId_1",
            false), Times.Once);
    }

    [TestMethod]
    public async Task UpgradeProcess_IndexFails_ReturnsFalse()
    {
        _context.Setup(x => x.CreateIndex(_repository, It.IsAny<OrderBuilder<MerchandiseReturn>>(), "OrderId_1",
            false)).ThrowsAsync(new InvalidOperationException());

        Assert.IsFalse(await new MigrationMerchandiseReturnOrderIndex().UpgradeProcess(_serviceProvider));
    }
}
