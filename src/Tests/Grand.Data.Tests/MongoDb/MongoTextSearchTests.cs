using Grand.Data.Mongo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;

namespace Grand.Data.Tests.MongoDb;

[TestClass]
public class MongoTextSearchTests
{
    private const string IndexName = "SampleCollection_Text";

    private MongoDBRepositoryTest<SampleCollection> _repository;
    private MongoDBContext _context;

    [TestInitialize]
    public async Task Init()
    {
        _repository = new MongoDBRepositoryTest<SampleCollection>();
        _context = new MongoDBContext(
            DriverTestConfiguration.Client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName));

        await _repository.InsertAsync(new SampleCollection { Name = "Smartphone X200", Count = 1 });
        await _repository.InsertAsync(new SampleCollection { Name = "Phone case", Count = 2 });
        await _repository.InsertAsync(new SampleCollection { Name = "Phone holder", Count = 0 });
        await _context.CreateTextIndex(_repository, ["Name"], IndexName);
    }

    [TestMethod]
    public async Task CreateTextIndex_SameSpecTwice_DoesNotThrow()
    {
        await _context.CreateTextIndex(_repository, ["Name"], IndexName);

        var index = (await _repository.Collection.Indexes.ListAsync()).ToList()
            .Single(x => x["name"] == IndexName);
        Assert.AreEqual("none", index["default_language"].AsString);
    }

    [TestMethod]
    public async Task CreateTextIndex_FieldsChanged_ThrowsAndKeepsTheIndex()
    {
        await Assert.ThrowsAsync<MongoCommandException>(() =>
            _context.CreateTextIndex(_repository, ["Name", "Phones"], IndexName));

        Assert.IsTrue(await _context.TextIndexExists(_repository, IndexName));
    }

    [TestMethod]
    public async Task CreateTextIndex_AnotherTextIndex_ThrowsAndKeepsIt()
    {
        await Assert.ThrowsAsync<MongoCommandException>(() =>
            _context.CreateTextIndex(_repository, ["Name", "Phones"], IndexName + "_v2"));

        Assert.IsTrue(await _context.TextIndexExists(_repository, IndexName));
        Assert.IsFalse(await _context.TextIndexExists(_repository, IndexName + "_v2"));
    }

    [TestMethod]
    public async Task TextIndexExists_Built_True()
    {
        Assert.IsTrue(await _context.TextIndexExists(_repository, IndexName));
    }

    [TestMethod]
    public async Task TextIndexExists_NoSuchIndex_False()
    {
        await _context.DeleteIndex(_repository, IndexName);

        Assert.IsFalse(await _context.TextIndexExists(_repository, IndexName));
    }

    [TestMethod]
    public void TextSearch_FindsWholeWord_NotFragment()
    {
        var phone = _context.TextSearch(_repository, "phone").Select(x => x.Name).ToList();
        var fragment = _context.TextSearch(_repository, "phon").ToList();

        CollectionAssert.AreEquivalent(new[] { "Phone case", "Phone holder" }, phone);
        Assert.AreEqual(0, fragment.Count);
    }

    [TestMethod]
    public async Task TextSearch_WithFurtherWhereAndCount()
    {
        var query = _context.TextSearch(_repository, "phone").Where(x => x.Count > 0);

        var page = await _repository.PagedAsync(query, 0, 10);

        Assert.AreEqual(1, page.TotalCount);
        Assert.AreEqual("Phone case", page.Single().Name);
    }
}
