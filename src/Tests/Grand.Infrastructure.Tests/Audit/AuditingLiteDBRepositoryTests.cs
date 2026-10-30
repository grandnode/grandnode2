using Grand.Data;
using Grand.Data.Audit;
using Grand.Data.LiteDb;
using Grand.Domain;
using Grand.Domain.Audit;
using Grand.Infrastructure.Audit;
using Grand.Mediator;
using LiteDB;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Infrastructure.Tests.Audit;

[TestClass]
public class AuditingLiteDBRepositoryTests
{
    private static readonly AuditActor Actor = new(AuditSource.Admin, "c1", "anna@shop.com", null, null, null,
        "10.0.0.1", "UA", "Admin / Product / Edit", "op-1");

    private LiteDatabase _database;
    private AuditingLiteDBRepository<Gadget> _repository;
    private AuditingLiteDBRepository<AuditEntry> _auditRepository;
    private AuditTracker _tracker;
    private List<AuditEntry> _written;

    [TestInitialize]
    public void Init()
    {
        _database = new LiteDatabase(new MemoryStream());
        _written = [];
        var writer = new Mock<IAuditEntryWriter>();
        writer.Setup(w => w.InsertManyAsync(It.IsAny<IReadOnlyCollection<AuditEntry>>()))
            .Callback<IReadOnlyCollection<AuditEntry>>(e => _written.AddRange(e))
            .Returns(Task.CompletedTask);
        _tracker = new AuditTracker(writer.Object, new Mock<IMediator>().Object, new Mock<IAuditRecordReader>().Object,
            NullLogger<AuditTracker>.Instance);
        var auditInfo = new Mock<IAuditInfoProvider>();
        auditInfo.Setup(a => a.GetCurrentDateTime()).Returns(DateTime.UtcNow);
        _repository = new AuditingLiteDBRepository<Gadget>(_database, auditInfo.Object, _tracker);
        _auditRepository = new AuditingLiteDBRepository<AuditEntry>(_database, auditInfo.Object, _tracker);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _database.Dispose();
    }

    private async Task<Gadget> Seed()
    {
        var gadget = new Gadget { Name = "Lamp", Price = 10, Items = [new GadgetItem { Id = "i1", Qty = 1 }] };
        await _repository.InsertAsync(gadget);
        return gadget;
    }

    [TestMethod]
    public async Task Inactive_WritesPassThrough_NoEntries()
    {
        var gadget = await Seed();
        await _repository.UpdateField(gadget.Id, x => x.Price, 20);
        await _tracker.FlushAsync();

        Assert.AreEqual(0, _written.Count);
        Assert.AreEqual(20, (await _repository.GetByIdAsync(gadget.Id)).Price);
    }

    [TestMethod]
    public async Task UpdateField_Then_Flush_UpdateEntry()
    {
        var gadget = await Seed();
        _tracker.Activate(Actor);

        await _repository.UpdateField(gadget.Id, x => x.Price, 20);
        await _tracker.FlushAsync();

        var entry = _written.Single();
        Assert.AreEqual(AuditKind.Update, entry.Kind);
        Assert.AreEqual("Price", entry.Changes.Single().Path);
        Assert.AreEqual("20", entry.Changes.Single().NewValue);
    }

    [TestMethod]
    public async Task UpdateCollectionFieldItem_DiffPathById()
    {
        var gadget = await Seed();
        _tracker.Activate(Actor);

        await _repository.UpdateCollectionFieldItem(gadget.Id, x => x.Items, i => i.Id == "i1",
            new GadgetItem { Id = "i1", Qty = 5 });
        await _tracker.FlushAsync();

        var change = _written.Single().Changes.Single();
        Assert.AreEqual("Items[id=i1].Qty", change.Path);
        Assert.AreEqual("1", change.OldValue);
        Assert.AreEqual("5", change.NewValue);
    }

    [TestMethod]
    public async Task InsertAsync_InsertEntryWithId()
    {
        _tracker.Activate(Actor);

        var gadget = await Seed();
        await _tracker.FlushAsync();

        var entry = _written.Single();
        Assert.AreEqual(AuditKind.Insert, entry.Kind);
        Assert.AreEqual(gadget.Id, entry.EntityId);
        Assert.AreEqual("Lamp", entry.EntityName);
    }

    [TestMethod]
    public async Task DeleteAsync_DeleteEntry()
    {
        var gadget = await Seed();
        _tracker.Activate(Actor);

        await _repository.DeleteAsync(gadget);
        await _tracker.FlushAsync();

        var entry = _written.Single();
        Assert.AreEqual(AuditKind.Delete, entry.Kind);
        Assert.AreEqual(gadget.Id, entry.EntityId);
    }

    [TestMethod]
    public async Task UpdateAsync_FullUpdate_UpdateEntry()
    {
        var gadget = await Seed();
        _tracker.Activate(Actor);

        gadget.Name = "Desk lamp";
        await _repository.UpdateAsync(gadget);
        await _tracker.FlushAsync();

        Assert.AreEqual("Name", _written.Single().Changes.Single().Path);
    }

    [TestMethod]
    public async Task UpdateOneAsync_ByIdFilter_EntityEntry()
    {
        var gadget = await Seed();
        _tracker.Activate(Actor);

        await _repository.UpdateOneAsync(x => x.Id == gadget.Id, UpdateBuilder<Gadget>.Create().Set(x => x.Price, 30));
        await _tracker.FlushAsync();

        var entry = _written.Single();
        Assert.AreEqual(AuditKind.Update, entry.Kind);
        Assert.AreEqual("Price", entry.Changes.Single().Path);
    }

    [TestMethod]
    public async Task UpdateManyAsync_BulkEntry()
    {
        await Seed();
        _tracker.Activate(Actor);

        await _repository.UpdateManyAsync(x => x.Price > 5, UpdateBuilder<Gadget>.Create().Set(x => x.Price, 1));
        await _tracker.FlushAsync();

        var entry = _written.Single();
        Assert.AreEqual(AuditKind.Bulk, entry.Kind);
        StringAssert.Contains(entry.Details, "UpdateManyAsync");
    }

    [TestMethod]
    public async Task AuditIndexes_CreateOnLiteDb_Twice()
    {
        var context = new LiteDBContext(_database);

        await AuditIndexes.Create(context, _auditRepository);
        await AuditIndexes.Create(context, _auditRepository);

        var indexes = _database.GetCollection("$indexes").Query().Where("collection = @0", nameof(AuditEntry)).ToList();
        Assert.IsTrue(indexes.Count >= 5, string.Join(", ", indexes.Select(i => i["name"].AsString)));
    }

    [TestMethod]
    public async Task ExcludedType_NoEntry()
    {
        _tracker.Activate(Actor);

        await _auditRepository.InsertAsync(new AuditEntry { Details = "x" });
        await _tracker.FlushAsync();

        Assert.AreEqual(0, _written.Count);
    }

    [TestMethod]
    public async Task RecordReader_ReadMany_ByIdWithoutMissing()
    {
        var lamp = await Seed();
        var desk = new Gadget { Name = "Desk" };
        await _repository.InsertAsync(desk);

        var records = await new LiteDBAuditRecordReader(() => _database)
            .ReadMany(typeof(Gadget), [lamp.Id, desk.Id, "missing"], null, "Name");

        Assert.AreEqual(2, records.Count);
        Assert.AreEqual("Lamp", records[lamp.Id]["Name"]);
        Assert.AreEqual("Desk", records[desk.Id]["Name"]);
    }

    public class Gadget : BaseEntity
    {
        public string Name { get; set; }
        public int Price { get; set; }
        public IList<GadgetItem> Items { get; set; } = [];
    }

    public class GadgetItem
    {
        public string Id { get; set; }
        public int Qty { get; set; }
    }
}
