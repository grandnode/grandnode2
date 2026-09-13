using Grand.Data.Mongo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;

namespace Grand.Data.Tests.MongoDb;

/// <summary>
///     <see cref="MongoRepository{T}" /> executes whatever query it is handed straight on the driver, with no check
///     that the query is still server side. That is only safe because every shape the services compose on top of
///     <see cref="IRepository{T}.Table" /> stays a driver query - projections, groupings and sub-collection
///     flattening included. These tests pin that assumption: if a driver upgrade breaks one of them, the matching
///     service starts throwing instead of quietly materialising the whole collection.
/// </summary>
[TestClass]
public class MongoQueryableContractTests
{
    private IRepository<SampleCollection> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<SampleCollection>();
    }

    private static void AssertServerSide<T>(IQueryable<T> query)
    {
        Assert.IsInstanceOfType<IAsyncCursorSource<T>>(query);
    }

    [TestMethod]
    public void Table_StaysServerSide()
    {
        AssertServerSide(_repository.Table);
    }

    [TestMethod]
    public void FilterSortPage_StaysServerSide()
    {
        AssertServerSide(_repository.Table.Where(x => x.Count > 0).OrderBy(x => x.Name).Skip(10).Take(10));
    }

    [TestMethod]
    public void ProjectionToScalar_StaysServerSide()
    {
        AssertServerSide(_repository.Table.Select(x => x.Name));
    }

    /// <summary>Shape used by CustomerReportService.GetBestCustomersReport.</summary>
    [TestMethod]
    public void GroupingToAnonymousType_StaysServerSide()
    {
        var grouped = from item in _repository.Table
            group item by item.Name
            into g
            select new { Name = g.Key, Total = g.Sum(x => x.Count), Lines = g.Count() };

        AssertServerSide(grouped.OrderByDescending(x => x.Total));
    }

    /// <summary>Shape used by ProductCategoryService and ProductCollectionService.</summary>
    [TestMethod]
    public void FlattenedSubCollection_StaysServerSide()
    {
        var flattened = from sample in _repository.Table
            from category in sample.Category
            select new { sample.Id, category.Name, category.DisplayOrder };

        AssertServerSide(flattened.Where(x => x.DisplayOrder > 0).OrderBy(x => x.DisplayOrder));
    }

    /// <summary>Shape used by OrderReportService - the closure list is sent as a filter, not enumerated locally.</summary>
    [TestMethod]
    public void FilterAgainstInMemoryList_StaysServerSide()
    {
        var excluded = new List<string> { "a", "b" };

        AssertServerSide(_repository.Table.Where(x => !excluded.Contains(x.Id)));
    }

    [TestMethod]
    public void PlainLinqToObjects_IsNotServerSide()
    {
        Assert.IsNotInstanceOfType<IAsyncCursorSource<SampleCollection>>(new List<SampleCollection>().AsQueryable());
    }
}
