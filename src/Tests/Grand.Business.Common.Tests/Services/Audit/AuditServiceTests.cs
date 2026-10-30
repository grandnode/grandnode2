using Grand.Business.Common.Services.Audit;
using Grand.Business.Core.Interfaces.Common.Audit;
using Grand.Data.Audit;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Audit;
using Grand.Infrastructure.Audit;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Common.Tests.Services.Audit;

[TestClass]
public class AuditServiceTests
{
    private MongoDBRepositoryTest<AuditEntry> _repository;
    private Mock<IAuditEntryWriter> _writer;
    private Mock<IMediator> _mediator;
    private AuditService _service;

    [TestInitialize]
    public async Task Init()
    {
        _repository = new MongoDBRepositoryTest<AuditEntry>();
        await _repository.ClearAsync();
        _writer = new Mock<IAuditEntryWriter>();
        _mediator = new Mock<IMediator>();
        _service = new AuditService(_repository, _writer.Object, _mediator.Object,
            new MemoryCacheBase(MemoryCacheTest.Get(), _mediator.Object, new CacheConfig { DefaultCacheTimeMinutes = 1 }));
    }

    private async Task<AuditEntry> Add(string entityType, string entityId, DateTime createdOnUtc,
        string email = "anna@shop.com")
    {
        var entry = new AuditEntry {
            Kind = AuditKind.Update, EntityType = entityType, EntityId = entityId, ActorEmail = email
        };
        await _repository.InsertAsync(entry);
        entry.CreatedOnUtc = createdOnUtc;
        await _repository.UpdateAsync(entry);
        return entry;
    }

    [TestMethod]
    public async Task GetEntries_FiltersByEntityTypeAndId_NewestFirst()
    {
        var older = await Add("Product", "p1", new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = await Add("Product", "p1", new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc));
        await Add("Product", "p2", new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc));
        await Add("Category", "p1", new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc));

        var result = await _service.GetEntries(new AuditEntrySearch { EntityType = "Product", EntityId = "p1" });

        CollectionAssert.AreEqual(new[] { newer.Id, older.Id }, result.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task GetEntries_RecordHistory_HoldsTheRecordsThatBelongToIt()
    {
        var update = await Add("MerchandiseReturn", "r1", new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        var note = await Add("MerchandiseReturnNote", "n1", new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc));
        note.OwnerType = "MerchandiseReturn";
        note.OwnerId = "r1";
        await _repository.UpdateAsync(note);
        var otherNote = await Add("MerchandiseReturnNote", "n2", new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc));
        otherNote.OwnerType = "MerchandiseReturn";
        otherNote.OwnerId = "r2";
        await _repository.UpdateAsync(otherNote);

        var result = await _service.GetEntries(new AuditEntrySearch { EntityType = "MerchandiseReturn", EntityId = "r1" });

        CollectionAssert.AreEqual(new[] { note.Id, update.Id }, result.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task GetEntries_SeveralEntityTypes()
    {
        var permission = await Add("Permission", "p1", new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        var action = await Add("PermissionAction", "a1", new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc));
        await Add("Product", "x1", new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc));

        var result = await _service.GetEntries(new AuditEntrySearch { EntityType = "Permission, PermissionAction" });

        CollectionAssert.AreEqual(new[] { action.Id, permission.Id }, result.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task GetEntries_FiltersByEntityNames()
    {
        var catalog = await Add("Setting", "s1", DateTime.UtcNow);
        catalog.EntityName = "catalogsettings";
        await _repository.UpdateAsync(catalog);
        var order = await Add("Setting", "s2", DateTime.UtcNow);
        order.EntityName = "ordersettings";
        await _repository.UpdateAsync(order);

        var result = await _service.GetEntries(new AuditEntrySearch {
            EntityType = "Setting", EntityNames = ["catalogsettings", "mediasettings"]
        });

        Assert.AreEqual(catalog.Id, result.Single().Id);
    }

    [TestMethod]
    public async Task GetEntries_SearchText_StaffEmailCaseInsensitive()
    {
        var anna = await Add("Product", "p1", DateTime.UtcNow, "Anna@Shop.com");
        await Add("Product", "p1", DateTime.UtcNow, "marek@shop.com");

        var result = await _service.GetEntries(new AuditEntrySearch { SearchText = "anna@" });

        Assert.AreEqual(anna.Id, result.Single().Id);
    }

    [TestMethod]
    public async Task GetEntries_SearchText_RecordName()
    {
        var lamp = await Add("Product", "p1", DateTime.UtcNow);
        lamp.EntityName = "Aurora Lamp";
        await _repository.UpdateAsync(lamp);
        await Add("Product", "p2", DateTime.UtcNow);

        var result = await _service.GetEntries(new AuditEntrySearch { SearchText = "aurora" });

        Assert.AreEqual(lamp.Id, result.Single().Id);
    }

    [TestMethod]
    public async Task GetEntries_DateRange()
    {
        await Add("Product", "p1", new DateTime(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc));
        var inside = await Add("Product", "p1", new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc));
        await Add("Product", "p1", new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc));

        var result = await _service.GetEntries(new AuditEntrySearch {
            FromUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            ToUtc = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc)
        });

        Assert.AreEqual(inside.Id, result.Single().Id);
    }

    [TestMethod]
    public async Task StreamEntries_YieldsAllAcrossPages()
    {
        for (var i = 0; i < 1005; i++)
            await _repository.InsertAsync(new AuditEntry { EntityType = "Product", CreatedOnUtc = DateTime.UtcNow });

        var count = 0;
        await foreach (var _ in _service.StreamEntries(new AuditEntrySearch(), CancellationToken.None))
            count++;

        Assert.AreEqual(1005, count);
    }

    [TestMethod]
    public async Task StreamEntries_SameTimeAcrossPages_EachEntryOnce()
    {
        //one flush stamps all its entries with the same time
        var time = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 2100; i++)
            await _repository.InsertAsync(new AuditEntry { EntityType = "Product", CreatedOnUtc = i < 1500 ? time : time.AddMinutes(-1) });

        var ids = new List<string>();
        await foreach (var entry in _service.StreamEntries(new AuditEntrySearch(), CancellationToken.None))
            ids.Add(entry.Id);

        Assert.AreEqual(2100, ids.Count);
        Assert.AreEqual(2100, ids.Distinct().Count());
    }

    [TestMethod]
    public async Task GetEntries_Store_MatchesActorStoreOrRecordStore()
    {
        var byStoreStaff = await Add("Product", "p1", DateTime.UtcNow);
        byStoreStaff.StoreId = "s2";
        await _repository.UpdateAsync(byStoreStaff);
        var adminOnStoreRecord = await Add("Product", "p2", DateTime.UtcNow);
        adminOnStoreRecord.EntityStoreId = "s2";
        await _repository.UpdateAsync(adminOnStoreRecord);
        await Add("Product", "p3", DateTime.UtcNow);

        var result = await _service.GetEntries(new AuditEntrySearch { StoreId = "s2" });

        CollectionAssert.AreEquivalent(new[] { byStoreStaff.Id, adminOnStoreRecord.Id },
            result.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task GetEntityTypes_DistinctSorted()
    {
        await Add("Product", "1", DateTime.UtcNow);
        await Add("Category", "1", DateTime.UtcNow);
        await Add("Product", "2", DateTime.UtcNow);

        CollectionAssert.AreEqual(new[] { "Category", "Product" }, (await _service.GetEntityTypes()).ToArray());
    }

    [TestMethod]
    public async Task DeleteOlderThan_RemovesOnlyOlder()
    {
        await Add("Product", "old", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var kept = await Add("Product", "new", new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));

        var deleted = await _service.DeleteOlderThan(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.AreEqual(1, deleted);
        Assert.AreEqual(kept.Id, _repository.Table.Single().Id);
    }

    [TestMethod]
    public async Task InsertEvent_WritesAndPublishes()
    {
        var entry = new AuditEntry { Kind = AuditKind.SignIn };

        await _service.InsertEvent(entry);

        _writer.Verify(w => w.InsertManyAsync(It.Is<IReadOnlyCollection<AuditEntry>>(e => e.Single() == entry)));
        _mediator.Verify(m => m.Publish(It.Is<AuditEntryCreated>(n => n.Entry == entry), It.IsAny<CancellationToken>()));
        Assert.AreNotEqual(default, entry.CreatedOnUtc);
    }

    [TestMethod]
    public void Service_HasNoUpdateOrDeleteById()
    {
        var names = typeof(IAuditService).GetMethods().Select(m => m.Name).ToList();

        Assert.IsFalse(names.Any(n => n.StartsWith("Update")));
        CollectionAssert.AreEqual(new[] { "DeleteOlderThan" }, names.Where(n => n.StartsWith("Delete")).ToArray());
    }
}
