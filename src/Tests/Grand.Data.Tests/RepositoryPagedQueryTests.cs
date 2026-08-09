using Grand.Data.Tests.LiteDb;
using Grand.Data.Tests.MongoDb;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Data.Tests;

/// <summary>
///     Covers the query execution primitives on <see cref="IRepository{T}" />. The paging arithmetic has to stay
///     identical to what the paged list produced before execution moved out of the domain project, so every
///     assertion here is on TotalCount / TotalPages / page contents rather than on how the query ran.
/// </summary>
[TestClass]
public class RepositoryPagedQueryTests
{
    private const string Mongo = "MongoDB";
    private const string LiteDb = "LiteDB";

    private IRepository<SampleCollection> _liteDb;
    private IRepository<SampleCollection> _mongoRepository;

    [TestInitialize]
    public async Task Init()
    {
        _mongoRepository = new MongoDBRepositoryTest<SampleCollection>();
        _liteDb = new LiteDBRepositoryMock<SampleCollection>();

        foreach (var repository in new[] { _mongoRepository, _liteDb })
            for (var i = 1; i <= 25; i++)
                await repository.InsertAsync(new SampleCollection { Name = $"sample {i:00}", Count = i });
    }

    private IRepository<SampleCollection> Repository(string provider)
    {
        return provider == Mongo ? _mongoRepository : _liteDb;
    }

    [TestMethod]
    [DataRow(Mongo)]
    [DataRow(LiteDb)]
    public async Task PagedAsync_FirstPage_ReturnsPageAndTotals(string provider)
    {
        var repository = Repository(provider);
        var query = repository.Table.OrderBy(x => x.Count);

        var result = await repository.PagedAsync(query, 0, 10);

        Assert.AreEqual(25, result.TotalCount);
        Assert.AreEqual(3, result.TotalPages);
        Assert.AreEqual(0, result.PageIndex);
        Assert.AreEqual(10, result.PageSize);
        Assert.AreEqual(10, result.Count);
        Assert.AreEqual(1, result.First().Count);
        Assert.IsFalse(result.HasPreviousPage);
        Assert.IsTrue(result.HasNextPage);
    }

    [TestMethod]
    [DataRow(Mongo)]
    [DataRow(LiteDb)]
    public async Task PagedAsync_LastPartialPage_ReturnsRemainder(string provider)
    {
        var repository = Repository(provider);
        var query = repository.Table.OrderBy(x => x.Count);

        var result = await repository.PagedAsync(query, 2, 10);

        Assert.AreEqual(25, result.TotalCount);
        Assert.AreEqual(5, result.Count);
        Assert.AreEqual(21, result.First().Count);
        Assert.IsTrue(result.HasPreviousPage);
        Assert.IsFalse(result.HasNextPage);
    }

    [TestMethod]
    [DataRow(Mongo)]
    [DataRow(LiteDb)]
    public async Task PagedAsync_PageBeyondRange_ReturnsEmptyPageWithTotals(string provider)
    {
        var repository = Repository(provider);
        var query = repository.Table.OrderBy(x => x.Count);

        var result = await repository.PagedAsync(query, 99, 10);

        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(25, result.TotalCount);
        Assert.AreEqual(3, result.TotalPages);
    }

    [TestMethod]
    [DataRow(Mongo)]
    [DataRow(LiteDb)]
    public async Task PagedAsync_NoMatches_ReturnsEmpty(string provider)
    {
        var repository = Repository(provider);
        var query = repository.Table.Where(x => x.Count > 1000);

        var result = await repository.PagedAsync(query, 0, 10);

        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(0, result.TotalCount);
        Assert.AreEqual(0, result.TotalPages);
    }

    [TestMethod]
    [DataRow(Mongo)]
    [DataRow(LiteDb)]
    public async Task PagedAsync_NonPositivePageSize_NormalizesToOne(string provider)
    {
        var repository = Repository(provider);
        var query = repository.Table.OrderBy(x => x.Count);

        var result = await repository.PagedAsync(query, 0, 0);

        Assert.AreEqual(1, result.PageSize);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(25, result.TotalPages);
    }

    [TestMethod]
    [DataRow(Mongo)]
    [DataRow(LiteDb)]
    public async Task PagedAsync_Projection_ReturnsProjectedPage(string provider)
    {
        var repository = Repository(provider);
        var query = repository.Table.OrderBy(x => x.Count).Select(x => x.Name);

        var result = await repository.PagedAsync(query, 0, 5);

        Assert.AreEqual(25, result.TotalCount);
        Assert.AreEqual(5, result.Count);
        Assert.AreEqual("sample 01", result.First());
    }

    [TestMethod]
    [DataRow(Mongo)]
    [DataRow(LiteDb)]
    public async Task ToListAsync_ReturnsAllMatches(string provider)
    {
        var repository = Repository(provider);

        var result = await repository.ToListAsync(repository.Table.Where(x => x.Count <= 3));

        Assert.AreEqual(3, result.Count);
    }

    [TestMethod]
    [DataRow(Mongo)]
    [DataRow(LiteDb)]
    public async Task CountAsync_ReturnsNumberOfMatches(string provider)
    {
        var repository = Repository(provider);

        Assert.AreEqual(3, await repository.CountAsync(repository.Table.Where(x => x.Count <= 3)));
    }

    [TestMethod]
    [DataRow(Mongo)]
    [DataRow(LiteDb)]
    public async Task PagedAsync_NullQuery_Throws(string provider)
    {
        var repository = Repository(provider);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() =>
            repository.PagedAsync<SampleCollection>(null, 0, 10));
    }

    /// <summary>
    ///     Handing the Mongo repository a sequence that is already in memory is a programming error - it means the
    ///     caller materialised the query before the repository could run it, which is the blocking pattern these
    ///     methods exist to remove. It has to fail loudly rather than quietly enumerate and look like it worked.
    /// </summary>
    [TestMethod]
    public async Task MongoRepository_InMemoryQuery_ThrowsInsteadOfEnumerating()
    {
        var items = Enumerable.Range(1, 25).Select(i => new SampleCollection { Count = i }).ToList();

        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            _mongoRepository.ToListAsync(items.AsQueryable().Where(x => x.Count <= 4)));
        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            _mongoRepository.PagedAsync(items.AsQueryable().OrderBy(x => x.Count), 0, 10));
    }
}
