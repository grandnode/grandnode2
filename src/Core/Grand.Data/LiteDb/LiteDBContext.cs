using Grand.Domain;
using Grand.Domain.Common;
using LiteDB;

namespace Grand.Data.LiteDb;

public class LiteDBContext : IDatabaseContext
{
    private readonly LiteDatabase _database;

    public LiteDBContext(LiteDatabase database)
    {
        _database = database;
    }
   
    public async Task<bool> DatabaseExist()
    {
        return await Task.FromResult(_database.CollectionExists(nameof(GrandNodeVersion)));
    }

    public Task CreateTable(string name, string collation)
    {
        //Not supported by LiteDB
        return Task.CompletedTask;
    }

    public Task DeleteTable(string name)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(name);

        _database.DropCollection(name);

        return Task.CompletedTask;
    }

    public Task CreateIndex<T>(IRepository<T> repository, OrderBuilder<T> orderBuilder, string indexName,
        bool unique = false) where T : BaseEntity
    {
        ArgumentNullException.ThrowIfNullOrEmpty(indexName);
        //duplicates on a unique index must reach the caller
        var col = _database.GetCollection<T>();
        foreach (var (selector, _, _) in orderBuilder.Fields)
            if (selector != null)
                col.EnsureIndex(selector, unique);

        return Task.CompletedTask;
    }

    public Task CreateTextIndex<T>(IRepository<T> repository, string[] fieldNames, string indexName,
        CancellationToken cancellationToken = default) where T : BaseEntity
    {
        //Not supported by LiteDB
        return Task.CompletedTask;
    }

    public Task<bool> TextIndexExists<T>(IRepository<T> repository, string indexName) where T : BaseEntity
    {
        return Task.FromResult(false);
    }

    public IQueryable<T> TextSearch<T>(IRepository<T> repository, string search) where T : BaseEntity
    {
        //no text index to narrow by; the caller's own filters decide the match
        return repository.Table;
    }

    public Task DeleteIndex<T>(IRepository<T> repository, string indexName) where T : BaseEntity
    {
        if (string.IsNullOrEmpty(indexName))
            throw new ArgumentNullException(nameof(indexName));
        //returns false, does not throw, when there is no such index
        _database.GetCollection<T>().DropIndex(indexName);

        return Task.CompletedTask;
    }
}