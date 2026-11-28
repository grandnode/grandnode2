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
public class MigrationRecurringPaymentCreatedOnIndexTests
{
    private readonly Mock<IDatabaseContext> _context = new();
    private readonly LiteDBRepositoryMock<RecurringPayment> _repository = new();
    private IServiceProvider _serviceProvider;

    public MigrationRecurringPaymentCreatedOnIndexTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepository<RecurringPayment>>(_repository);
        services.AddSingleton(_context.Object);
        services.AddSingleton<ILogger<MigrationRecurringPaymentCreatedOnIndex>>(
            NullLogger<MigrationRecurringPaymentCreatedOnIndex>.Instance);
        _serviceProvider = services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task UpgradeProcess_CreatesCreatedOnIndex()
    {
        Assert.IsTrue(await new MigrationRecurringPaymentCreatedOnIndex().UpgradeProcess(_serviceProvider));

        _context.Verify(x => x.CreateIndex(_repository, It.IsAny<OrderBuilder<RecurringPayment>>(), "CreatedOnUtc",
            false), Times.Once);
    }

    [TestMethod]
    public async Task UpgradeProcess_IndexFails_ReturnsFalse()
    {
        _context.Setup(x => x.CreateIndex(_repository, It.IsAny<OrderBuilder<RecurringPayment>>(), "CreatedOnUtc",
            false)).ThrowsAsync(new InvalidOperationException());

        Assert.IsFalse(await new MigrationRecurringPaymentCreatedOnIndex().UpgradeProcess(_serviceProvider));
    }
}
