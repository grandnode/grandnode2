using Grand.Data.Mongo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;

namespace Grand.Data.Tests.MongoDb;

[TestClass]
public class MongoDBContextTests
{
    private MongoDBRepositoryTest<SampleCollection> _repository;
    private MongoDBContext _context;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<SampleCollection>();
        _context = new MongoDBContext(
            DriverTestConfiguration.Client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName));
    }

    [TestMethod]
    public async Task CreateIndex_SameSpecTwice_DoesNotThrow()
    {
        var keys = OrderBuilder<SampleCollection>.Create().Ascending(x => x.Count);

        await _context.CreateIndex(_repository, keys, "Count", true);
        await _context.CreateIndex(_repository, keys, "Count", true);

        Assert.IsTrue(IsUnique("Count"));
    }

    [TestMethod]
    public async Task CreateIndex_UniqueOnDuplicates_Throws()
    {
        await _repository.InsertAsync(new SampleCollection { Count = 7 });
        await _repository.InsertAsync(new SampleCollection { Count = 7 });

        await Assert.ThrowsAsync<MongoException>(() =>
            _context.CreateIndex(_repository, OrderBuilder<SampleCollection>.Create().Ascending(x => x.Count), "Count",
                true));

        Assert.IsFalse(IndexExists("Count"));
    }

    [TestMethod]
    public async Task CreateIndex_SameNameAnotherSpec_Throws()
    {
        await _context.CreateIndex(_repository, OrderBuilder<SampleCollection>.Create().Ascending(x => x.Count), "Count");

        await Assert.ThrowsAsync<MongoException>(() =>
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
    public async Task DeleteIndex_NoSuchCollection_DoesNotThrow()
    {
        await _context.DeleteIndex(_repository, "Count");
    }

    [TestMethod]
    public async Task DeleteIndex_ExistingIndex_DropsIt()
    {
        await _context.CreateIndex(_repository, OrderBuilder<SampleCollection>.Create().Ascending(x => x.Count), "Count");

        await _context.DeleteIndex(_repository, "Count");

        Assert.IsFalse(IndexExists("Count"));
    }

    private bool IndexExists(string indexName)
    {
        return _repository.Collection.Indexes.List().ToList().Any(x => x["name"] == indexName);
    }

    private bool IsUnique(string indexName)
    {
        return _repository.Collection.Indexes.List().ToList()
            .Any(x => x["name"] == indexName && x.Contains("unique") && x["unique"].ToBoolean());
    }
}
