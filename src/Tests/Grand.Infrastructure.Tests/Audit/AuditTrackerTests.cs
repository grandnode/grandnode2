using Grand.Data.Audit;
using Grand.Domain;
using Grand.Domain.Audit;
using Grand.Infrastructure.Audit;
using Grand.Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Infrastructure.Tests.Audit;

[TestClass]
public class AuditTrackerTests
{
    private readonly Dictionary<string, TrackedThing> _store = new();
    private int _loadCalls;
    private Func<IReadOnlyCollection<string>, Task<IList<TrackedThing>>> _access;
    private Mock<IMediator> _mediator;
    private CapturingWriter _writer;
    private AuditTracker _tracker;

    private static readonly AuditActor Actor = new(AuditSource.Admin, "c1", "anna@shop.com", null, "s1", null,
        "10.0.0.1", "UA", "Admin / Product / Edit", "op-1");

    [TestInitialize]
    public void Init()
    {
        _writer = new CapturingWriter();
        _mediator = new Mock<IMediator>();
        _tracker = new AuditTracker(_writer, _mediator.Object, NullLogger<AuditTracker>.Instance);
        _access = ids =>
        {
            _loadCalls++;
            IList<TrackedThing> found = ids.Where(_store.ContainsKey).Select(id => Clone(_store[id])).ToList();
            return Task.FromResult(found);
        };
    }

    private static TrackedThing Clone(TrackedThing t)
    {
        return new TrackedThing { Id = t.Id, Name = t.Name, Price = t.Price, StockQuantity = t.StockQuantity, Ticks = t.Ticks, StoreId = t.StoreId };
    }

    private TrackedThing Put(string id, string name = "Shoes", int price = 10, int stock = 5)
    {
        var thing = new TrackedThing { Id = id, Name = name, Price = price, StockQuantity = stock };
        _store[id] = thing;
        return thing;
    }

    private List<AuditEntry> Written => _writer.Batches.SelectMany(b => b).ToList();

    [TestMethod]
    public async Task Inactive_BeforeWrite_DoesNotRead()
    {
        Put("1");

        await _tracker.BeforeWriteAsync("1", false, _access);
        _store["1"].Price = 20;
        await _tracker.FlushAsync();

        Assert.AreEqual(0, _loadCalls);
        Assert.AreEqual(0, Written.Count);
    }

    [TestMethod]
    public async Task Update_ReadOnceAcrossWrites()
    {
        _tracker.Activate(Actor);
        Put("1");

        await _tracker.BeforeWriteAsync("1", false, _access, ["Name"]);
        await _tracker.BeforeWriteAsync("1", false, _access, ["Price"]);

        Assert.AreEqual(1, _loadCalls);
    }

    [TestMethod]
    public async Task Update_Flush_WritesOneEntryWithDiffAndActor()
    {
        _tracker.Activate(Actor);
        Put("1");

        await _tracker.BeforeWriteAsync("1", false, _access, ["Price"]);
        _store["1"].Price = 20;
        await _tracker.FlushAsync();

        var entry = Written.Single();
        Assert.AreEqual(AuditKind.Update, entry.Kind);
        Assert.AreEqual(AuditSource.Admin, entry.Source);
        Assert.AreEqual("c1", entry.ActorCustomerId);
        Assert.AreEqual("anna@shop.com", entry.ActorEmail);
        Assert.AreEqual("s1", entry.StoreId);
        Assert.AreEqual("10.0.0.1", entry.IpAddress);
        Assert.AreEqual("Admin / Product / Edit", entry.Action);
        Assert.AreEqual("op-1", entry.OperationId);
        Assert.AreEqual(nameof(TrackedThing), entry.EntityType);
        Assert.AreEqual("1", entry.EntityId);
        Assert.AreEqual("Shoes", entry.EntityName);
        Assert.AreEqual("Price", entry.Changes.Single().Path);
        Assert.AreEqual("10", entry.Changes.Single().OldValue);
        Assert.AreEqual("20", entry.Changes.Single().NewValue);
    }

    [TestMethod]
    public async Task Update_NoRealChange_NoEntry()
    {
        _tracker.Activate(Actor);
        Put("1");

        await _tracker.BeforeWriteAsync("1", false, _access, ["Price"]);
        await _tracker.FlushAsync();

        Assert.AreEqual(0, Written.Count);
    }

    [TestMethod]
    public async Task Update_OnlyConcurrencyTicksChanged_NoEntry()
    {
        _tracker.Activate(Actor);
        Put("1");

        await _tracker.BeforeWriteAsync("1", false, _access);
        _store["1"].Ticks = 638_000_000_000_000_000;
        await _tracker.FlushAsync();

        Assert.AreEqual(0, Written.Count);
    }

    [TestMethod]
    public async Task Update_RecordsTheStoreTheRecordBelongsTo()
    {
        _tracker.Activate(Actor);
        Put("1").StoreId = "";
        Put("2").StoreId = "s2";

        await _tracker.BeforeWriteAsync("1", false, _access, ["Price"]);
        await _tracker.BeforeWriteAsync("2", false, _access, ["Price"]);
        _store["1"].Price = 1;
        _store["2"].Price = 2;
        await _tracker.FlushAsync();

        Assert.AreEqual("", Written.Single(e => e.EntityId == "1").EntityStoreId);
        Assert.AreEqual("s2", Written.Single(e => e.EntityId == "2").EntityStoreId);
    }

    [TestMethod]
    public async Task FullUpdate_ThenDelete_KindDelete()
    {
        _tracker.Activate(Actor);
        Put("1");

        await _tracker.BeforeWriteAsync("1", false, _access);
        var updated = Clone(_store["1"]);
        updated.Price = 20;
        _store["1"] = updated;
        _tracker.AfterFullUpdate(updated);
        await _tracker.BeforeWriteAsync("1", false, _access);
        _store.Remove("1");
        await _tracker.FlushAsync();

        Assert.AreEqual(AuditKind.Delete, Written.Single().Kind);
    }

    [TestMethod]
    public async Task Insert_FullUpdate_Delete_InsertedAndDeletedDetails()
    {
        _tracker.Activate(Actor);
        Put("1");

        await _tracker.BeforeWriteAsync("1", true, _access);
        await _tracker.BeforeWriteAsync("1", false, _access);
        _tracker.AfterFullUpdate(_store["1"]);
        await _tracker.BeforeWriteAsync("1", false, _access);
        _store.Remove("1");
        await _tracker.FlushAsync();

        var entry = Written.Single();
        Assert.AreEqual(AuditKind.Delete, entry.Kind);
        Assert.AreEqual("inserted and deleted in the same operation", entry.Details);
    }

    [TestMethod]
    public async Task BeforeWrite_OnlyActivityFields_NoReadNoEntry()
    {
        _tracker.Activate(Actor);
        Put("1");

        await _tracker.BeforeWriteAsync("1", false, _access, ["LastActivityDateUtc", "LastIpAddress", "LastVisitedPage"]);
        await _tracker.FlushAsync();

        Assert.AreEqual(0, _loadCalls);
        Assert.AreEqual(0, Written.Count);
    }

    [TestMethod]
    public async Task BeforeRead_Throws_DoesNotFailTheWrite()
    {
        _tracker.Activate(Actor);
        Func<IReadOnlyCollection<string>, Task<IList<TrackedThing>>> failing =
            _ => throw new InvalidOperationException("db down");

        await _tracker.BeforeWriteAsync("1", false, failing, ["Price"]);
        await _tracker.FlushAsync();
    }

    [TestMethod]
    public async Task Insert_NoBeforeRead_KindInsert()
    {
        _tracker.Activate(Actor);
        Put("1");

        await _tracker.BeforeWriteAsync("1", true, _access);
        Assert.AreEqual(0, _loadCalls);
        await _tracker.FlushAsync();

        var entry = Written.Single();
        Assert.AreEqual(AuditKind.Insert, entry.Kind);
        Assert.IsTrue(entry.Changes.Any(c => c.Path == "Name" && c.NewValue == "Shoes"));
    }

    [TestMethod]
    public async Task Delete_KindDelete()
    {
        _tracker.Activate(Actor);
        Put("1");

        await _tracker.BeforeWriteAsync("1", false, _access);
        _store.Remove("1");
        await _tracker.FlushAsync();

        var entry = Written.Single();
        Assert.AreEqual(AuditKind.Delete, entry.Kind);
        Assert.AreEqual("Shoes", entry.EntityName);
    }

    [TestMethod]
    public async Task InsertThenDelete_SingleDeleteEntryWithDetails()
    {
        _tracker.Activate(Actor);
        Put("1");

        await _tracker.BeforeWriteAsync("1", true, _access);
        await _tracker.BeforeWriteAsync("1", false, _access);
        _store.Remove("1");
        await _tracker.FlushAsync();

        var entry = Written.Single();
        Assert.AreEqual(AuditKind.Delete, entry.Kind);
        Assert.AreEqual("inserted and deleted in the same operation", entry.Details);
    }

    [TestMethod]
    public async Task Batch_At200_FlushesBeforeTouching201st()
    {
        _tracker.Activate(Actor);
        for (var i = 0; i < 201; i++)
            Put(i.ToString());

        for (var i = 0; i < 200; i++)
        {
            await _tracker.BeforeWriteAsync(i.ToString(), false, _access, ["Price"]);
            _store[i.ToString()].Price = 99;
        }

        Assert.AreEqual(0, _writer.Batches.Count);
        await _tracker.BeforeWriteAsync("200", false, _access, ["Price"]);

        Assert.AreEqual(1, _writer.Batches.Count);
        Assert.AreEqual(200, _writer.Batches[0].Count);
    }

    [TestMethod]
    public async Task Retouch_AfterFlush_SecondEntryUsesFreshBefore()
    {
        _tracker.Activate(Actor);
        Put("1");
        await _tracker.BeforeWriteAsync("1", false, _access, ["Price"]);
        _store["1"].Price = 20;
        await _tracker.FlushAsync();

        await _tracker.BeforeWriteAsync("1", false, _access, ["Price"]);
        _store["1"].Price = 30;
        await _tracker.FlushAsync();

        Assert.AreEqual(2, Written.Count);
        Assert.AreEqual("20", Written[1].Changes.Single().OldValue);
        Assert.AreEqual("30", Written[1].Changes.Single().NewValue);
    }

    [TestMethod]
    public async Task Flush_LoadsAfterOncePerType()
    {
        _tracker.Activate(Actor);
        Put("1");
        Put("2");
        await _tracker.BeforeWriteAsync("1", false, _access, ["Price"]);
        await _tracker.BeforeWriteAsync("2", false, _access, ["Price"]);
        _loadCalls = 0;

        await _tracker.FlushAsync();

        Assert.AreEqual(1, _loadCalls);
    }

    [TestMethod]
    public async Task Bulk_RecordedAsIs()
    {
        _tracker.Activate(Actor);

        _tracker.RecordBulk<TrackedThing>("UpdateManyAsync", "x => x.Price > 10");
        await _tracker.FlushAsync();

        var entry = Written.Single();
        Assert.AreEqual(AuditKind.Bulk, entry.Kind);
        Assert.AreEqual(nameof(TrackedThing), entry.EntityType);
        StringAssert.Contains(entry.Details, "UpdateManyAsync");
        StringAssert.Contains(entry.Details, "x.Price > 10");
        Assert.AreEqual("op-1", entry.OperationId);
    }

    [TestMethod]
    public void ExcludedType_NotAudited()
    {
        Assert.IsFalse(_tracker.IsAudited(typeof(AuditEntry)));
        Assert.IsTrue(_tracker.IsAudited(typeof(TrackedThing)));
    }

    [TestMethod]
    public async Task Flush_WriterThrows_Logged_NotRethrown()
    {
        _tracker.Activate(Actor);
        _writer.Throw = true;
        Put("1");
        await _tracker.BeforeWriteAsync("1", false, _access, ["Price"]);
        _store["1"].Price = 20;

        await _tracker.FlushAsync();
    }

    [TestMethod]
    public async Task Flush_PublishesNotificationPerEntry()
    {
        _tracker.Activate(Actor);
        Put("1");
        Put("2");
        await _tracker.BeforeWriteAsync("1", false, _access, ["Price"]);
        await _tracker.BeforeWriteAsync("2", false, _access, ["Price"]);
        _store["1"].Price = 20;
        _store["2"].Price = 30;

        await _tracker.FlushAsync();

        _mediator.Verify(m => m.Publish(It.IsAny<AuditEntryCreated>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [TestMethod]
    public async Task PartialWrite_ForeignChangeOnOtherPath_Ignored()
    {
        _tracker.Activate(Actor);
        Put("1");

        await _tracker.BeforeWriteAsync("1", false, _access, ["Name"]);
        _store["1"].Name = "Boots";
        _store["1"].StockQuantity = 4; //a customer bought one meanwhile
        await _tracker.FlushAsync();

        Assert.AreEqual("Name", Written.Single().Changes.Single().Path);
    }

    [TestMethod]
    public async Task PartialWrites_UnionOfPaths()
    {
        _tracker.Activate(Actor);
        Put("1");

        await _tracker.BeforeWriteAsync("1", false, _access, ["Name"]);
        await _tracker.BeforeWriteAsync("1", false, _access, ["Price"]);
        _store["1"].Name = "Boots";
        _store["1"].Price = 20;
        await _tracker.FlushAsync();

        CollectionAssert.AreEquivalent(new[] { "Name", "Price" },
            Written.Single().Changes.Select(c => c.Path).ToArray());
    }

    [TestMethod]
    public async Task FullUpdate_UsesInMemoryAfter_NoReadAtFlush()
    {
        _tracker.Activate(Actor);
        Put("1");

        await _tracker.BeforeWriteAsync("1", false, _access);
        var updated = Clone(_store["1"]);
        updated.Price = 20;
        _store["1"] = updated;
        _tracker.AfterFullUpdate(updated);
        _store["1"].StockQuantity = 4; //a customer bought one after the admin's save
        await _tracker.FlushAsync();

        Assert.AreEqual(1, _loadCalls);
        Assert.AreEqual("Price", Written.Single().Changes.Single().Path);
    }

    [TestMethod]
    public async Task FullUpdate_ThenPartial_ReReadsAtFlush()
    {
        _tracker.Activate(Actor);
        Put("1");

        await _tracker.BeforeWriteAsync("1", false, _access);
        var updated = Clone(_store["1"]);
        updated.Price = 20;
        _store["1"] = updated;
        _tracker.AfterFullUpdate(updated);
        await _tracker.BeforeWriteAsync("1", false, _access, ["Name"]);
        _store["1"].Name = "Boots";
        await _tracker.FlushAsync();

        Assert.AreEqual(2, _loadCalls);
        CollectionAssert.AreEquivalent(new[] { "Name", "Price" },
            Written.Single().Changes.Select(c => c.Path).ToArray());
    }

    [TestMethod]
    public async Task WriteByAuditEntryCreatedHandler_NotTracked()
    {
        _tracker.Activate(Actor);
        Put("1");
        Put("outbox");
        var handlerSawActive = true;
        _mediator.Setup(m => m.Publish(It.IsAny<AuditEntryCreated>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                handlerSawActive = _tracker.IsActive;
                await _tracker.BeforeWriteAsync("outbox", false, _access);
            });

        await _tracker.BeforeWriteAsync("1", false, _access, ["Price"]);
        _store["1"].Price = 20;
        await _tracker.CompleteAsync();

        Assert.IsFalse(handlerSawActive);
        Assert.AreEqual("1", Written.Single().EntityId);
    }

    [TestMethod]
    public async Task Complete_FlushesAndStopsTracking()
    {
        _tracker.Activate(Actor);
        Put("1");

        await _tracker.BeforeWriteAsync("1", false, _access, ["Price"]);
        _store["1"].Price = 20;
        await _tracker.CompleteAsync();
        var loadsAfterComplete = _loadCalls;
        await _tracker.BeforeWriteAsync("1", false, _access, ["Price"]);

        Assert.AreEqual(1, Written.Count);
        Assert.IsFalse(_tracker.IsActive);
        Assert.AreEqual(loadsAfterComplete, _loadCalls);
    }

    [TestMethod]
    public async Task AfterBatchFlushPublish_CallerStillTracked()
    {
        _tracker.Activate(Actor);
        for (var i = 0; i <= AuditTracker.BatchSize; i++)
        {
            Put(i.ToString());
            //the 201st write flushes the first 200 and publishes them before it is queued itself
            await _tracker.BeforeWriteAsync(i.ToString(), false, _access, ["Price"]);
            _store[i.ToString()].Price = 20;
        }

        Assert.IsTrue(_tracker.IsActive);
        await _tracker.FlushAsync();
        Assert.AreEqual(AuditTracker.BatchSize + 1, Written.Count);
    }

    [TestMethod]
    public async Task EntityName_FromAuditNameProperty()
    {
        _tracker.Activate(Actor);
        var account = new LabelledThing { Id = "a1", Email = "anna@shop.com", Note = "x" };
        Func<IReadOnlyCollection<string>, Task<IList<LabelledThing>>> load =
            _ => Task.FromResult<IList<LabelledThing>>([account]);

        await _tracker.BeforeWriteAsync("a1", true, load);
        await _tracker.FlushAsync();

        Assert.AreEqual("anna@shop.com", Written.Single().EntityName);
    }

    public class LabelledThing : BaseEntity
    {
        [AuditName]
        public string Email { get; set; }

        public string Note { get; set; }
    }

    public class TrackedThing : BaseEntity
    {
        public string Name { get; set; }
        public int Price { get; set; }
        public int StockQuantity { get; set; }
        public long Ticks { get; set; }
        public string StoreId { get; set; }
    }

    private class CapturingWriter : IAuditEntryWriter
    {
        public List<List<AuditEntry>> Batches { get; } = [];
        public bool Throw { get; set; }

        public Task InsertManyAsync(IReadOnlyCollection<AuditEntry> entries)
        {
            if (Throw)
                throw new InvalidOperationException("db down");
            Batches.Add(entries.ToList());
            return Task.CompletedTask;
        }
    }
}
