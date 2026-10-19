using Grand.Domain;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Grand.Data.Mongo;

public class MongoDBContext : IDatabaseContext
{
    //server error codes
    private const int NamespaceNotFound = 26;
    private const int IndexNotFound = 27;

    private readonly IMongoDatabase _database;

    public MongoDBContext(IMongoDatabase mongodatabase)
    {
        _database = mongodatabase;
    }
    
    public async Task<bool> DatabaseExist()
    {
        var filter = new BsonDocument("name", "GrandNodeVersion");
        var found = await _database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
        return await found.AnyAsync();
    }

    public async Task CreateTable(string name, string collation)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(name);

        if (!string.IsNullOrEmpty(collation))
        {
            var options = new CreateCollectionOptions {
                Collation = new Collation(collation)
            };
            await _database.CreateCollectionAsync(name, options);
        }
        else
        {
            await _database.CreateCollectionAsync(name);
        }
    }

    public async Task DeleteTable(string name)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(name);
        await _database.DropCollectionAsync(name);
    }

    public async Task CreateIndex<T>(IRepository<T> repository, OrderBuilder<T> orderBuilder, string indexName,
        bool unique = false) where T : BaseEntity
    {
        ArgumentNullException.ThrowIfNullOrEmpty(indexName);

        IList<IndexKeysDefinition<T>> keys = new List<IndexKeysDefinition<T>>();
        foreach (var item in orderBuilder.Fields)
            if (item.selector != null)
                keys.Add(item.value
                    ? Builders<T>.IndexKeys.Ascending(item.selector)
                    : Builders<T>.IndexKeys.Descending(item.selector));
            else
                keys.Add(item.value
                    ? Builders<T>.IndexKeys.Ascending(item.fieldName)
                    : Builders<T>.IndexKeys.Descending(item.fieldName));

        //an index of the same name and spec is a no-op for the server; anything else - duplicates on a unique
        //index, the same name with another spec - must reach the caller
        await ((MongoRepository<T>)repository).Collection.Indexes.CreateOneAsync(new CreateIndexModel<T>(
            Builders<T>.IndexKeys.Combine(keys),
            new CreateIndexOptions { Name = indexName, Unique = unique }));
    }

    public async Task CreateTextIndex<T>(IRepository<T> repository, string[] fieldNames, string indexName)
        where T : BaseEntity
    {
        ArgumentNullException.ThrowIfNullOrEmpty(indexName);
        ArgumentNullException.ThrowIfNull(fieldNames);

        //no stemming or stop words - product names are in many languages
        await ((MongoRepository<T>)repository).Collection.Indexes.CreateOneAsync(new CreateIndexModel<T>(
            Builders<T>.IndexKeys.Combine(fieldNames.Select(x => Builders<T>.IndexKeys.Text(x))),
            new CreateIndexOptions { Name = indexName, DefaultLanguage = "none" }));
    }

    public async Task DeleteIndex<T>(IRepository<T> repository, string indexName) where T : BaseEntity
    {
        ArgumentNullException.ThrowIfNullOrEmpty(indexName);
        try
        {
            await ((MongoRepository<T>)repository).Collection.Indexes.DropOneAsync(indexName);
        }
        catch (MongoCommandException ex) when (ex.Code is IndexNotFound or NamespaceNotFound)
        {
            //nothing to drop
        }
    }
}