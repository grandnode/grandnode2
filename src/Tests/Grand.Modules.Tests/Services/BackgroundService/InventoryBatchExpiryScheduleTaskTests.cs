using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Catalog;
using Grand.Domain.Stores;
using Grand.Module.ScheduledTasks.BackgroundServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Services.BackgroundService;

[TestClass]
public class InventoryBatchExpiryScheduleTaskTests
{
    private readonly Mock<ICatalogMessageProviderService> _messages = new();
    private readonly Mock<IProductUnitService> _units = new();
    private InventoryBatchExpiryScheduleTask _task;

    [TestInitialize]
    public void Init()
    {
        var stores = new Mock<IStoreService>();
        stores.Setup(x => x.GetAllStores()).ReturnsAsync([
            new Store { Id = "store2", DisplayOrder = 2, DefaultLanguageId = "en" },
            new Store { Id = "store1", DisplayOrder = 1, DefaultLanguageId = "en" }
        ]);
        var dateTime = new Mock<IDateTimeService>();
        dateTime.Setup(x => x.ConvertToUserTime(It.IsAny<DateTime>(), It.IsAny<DateTimeKind>()))
            .Returns((DateTime date, DateTimeKind _) => date);
        _messages.Setup(x => x.SendBatchesExpiringStoreOwnerMessage(It.IsAny<Store>(),
            It.IsAny<IList<ExpiringBatch>>(), It.IsAny<string>())).ReturnsAsync(1);
        _task = new InventoryBatchExpiryScheduleTask(_units.Object, stores.Object, _messages.Object, dateTime.Object);
    }

    private static ExpiringBatch Expiring(string storeId, BatchExpiryStatus status = BatchExpiryStatus.Expiring) =>
        new(new ProductBatch { StoreId = storeId, Quantity = 1 }, "p", "sku", "w", status);

    private void ToNotify(params ExpiringBatch[] batches) =>
        _units.Setup(x => x.BatchesToNotify(It.IsAny<DateTime>())).ReturnsAsync(batches);

    [TestMethod]
    public async Task Execute_ExpiringBatch_SentToItsStore_ThenMarked()
    {
        var batch = Expiring("store2");
        ToNotify(batch);

        await _task.Execute(CancellationToken.None);

        _messages.Verify(x => x.SendBatchesExpiringStoreOwnerMessage(It.Is<Store>(s => s.Id == "store2"),
            It.Is<IList<ExpiringBatch>>(b => b.Single() == batch), "en"), Times.Once);
        _units.Verify(x => x.MarkNotified(It.Is<IEnumerable<ExpiringBatch>>(b => b.Single() == batch)), Times.Once);
    }

    [TestMethod]
    public async Task Execute_SharedWarehouse_GoesToDefaultStore()
    {
        ToNotify(Expiring(""));

        await _task.Execute(CancellationToken.None);

        _messages.Verify(x => x.SendBatchesExpiringStoreOwnerMessage(It.Is<Store>(s => s.Id == "store1"),
            It.IsAny<IList<ExpiringBatch>>(), "en"), Times.Once);
    }

    [TestMethod]
    public async Task Execute_NoBatches_NothingSent()
    {
        ToNotify();

        await _task.Execute(CancellationToken.None);

        _messages.Verify(x => x.SendBatchesExpiringStoreOwnerMessage(It.IsAny<Store>(),
            It.IsAny<IList<ExpiringBatch>>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Execute_TemplateMissing_NotMarked()
    {
        ToNotify(Expiring("store1"));
        _messages.Setup(x => x.SendBatchesExpiringStoreOwnerMessage(It.IsAny<Store>(),
            It.IsAny<IList<ExpiringBatch>>(), It.IsAny<string>())).ReturnsAsync(0);

        await _task.Execute(CancellationToken.None);

        _units.Verify(x => x.MarkNotified(It.IsAny<IEnumerable<ExpiringBatch>>()), Times.Never);
    }
}
