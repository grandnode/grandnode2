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
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        _database.DropCollection(name);

        return Task.CompletedTask;
    }

    public Task CreateIndex<T>(IRepository<T> repository, OrderBuilder<T> orderBuilder, string indexName,
        bool unique = false) where T : BaseEntity
    {
        if (string.IsNullOrEmpty(indexName))
            throw new ArgumentNullException(nameof(indexName));
        try
        {
            foreach (var (selector, value, fieldName) in orderBuilder?.Fields)
            {
                var col = _database.GetCollection<T>();
                if (selector != null)
                    col.EnsureIndex(selector, unique);
            }
        }
        catch { }

        return Task.CompletedTask;
    }

    public Task DeleteIndex<T>(IRepository<T> repository, string indexName) where T : BaseEntity
    {
        if (string.IsNullOrEmpty(indexName))
            throw new ArgumentNullException(nameof(indexName));
        try
        {
            _database.GetCollection<T>().DropIndex(indexName);
        }
        catch { }

        return Task.CompletedTask;
    }
}