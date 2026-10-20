using Grand.Data.LiteDb;
using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Data.Tests.LiteDb;

[TestClass]
public class LiteDBContextTests
{
    private LiteDatabase _database;
    private LiteDBRepository<SampleCollection> _repository;
    private LiteDBContext _context;

    [TestInitialize]
    public void Init()
    {
        _database = new LiteDatabase(new MemoryStream());
        _repository = new LiteDBRepository<SampleCollection>(_database, new AuditInfoProvider());
        _context = new LiteDBContext(_database);
    }

    [TestCleanup]
    public void Cleanup() => _database.Dispose();

    [TestMethod]
    public async Task CreateIndex_SameSpecTwice_DoesNotThrow()
    {
        var keys = OrderBuilder<SampleCollection>.Create().Ascending(x => x.Count);

        await _context.CreateIndex(_repository, keys, "Count", true);
        await _context.CreateIndex(_repository, keys, "Count", true);

        await _repository.InsertAsync(new SampleCollection { Count = 7 });
        Assert.ThrowsExactly<LiteException>(() =>
            _database.GetCollection<SampleCollection>().Insert(new SampleCollection { Id = "2", Count = 7 }));
    }

    [TestMethod]
    public async Task CreateIndex_UniqueOnDuplicates_Throws()
    {
        await _repository.InsertAsync(new SampleCollection { Count = 7 });
        await _repository.InsertAsync(new SampleCollection { Count = 7 });

        await Assert.ThrowsExactlyAsync<LiteException>(() =>
            _context.CreateIndex(_repository, OrderBuilder<SampleCollection>.Create().Ascending(x => x.Count), "Count",
                true));
    }

    [TestMethod]
    public async Task DeleteIndex_NoSuchIndex_DoesNotThrow()
    {
        await _repository.InsertAsync(new SampleCollection { Count = 1 });

        await _context.DeleteIndex(_repository, "Count");
    }

    [TestMethod]
    public async Task TextIndex_NotSupported_SearchKeepsTheTable()
    {
        await _repository.InsertAsync(new SampleCollection { Name = "Smartphone" });

        await _context.CreateTextIndex(_repository, ["Name"], "Name_Text");

        Assert.AreEqual(1, _context.TextSearch(_repository, "phone").Count());
    }
}
