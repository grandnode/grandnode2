using Grand.Domain;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Grand.Data.Mongo;

public class MongoDBContext : IDatabaseContext
{
    //server error codes
    private const int NamespaceNotFound = 26;
    private const int IndexNotFound = 27;
    private const int IndexOptionsConflict = 85;
    private const int IndexKeySpecsConflict = 86;

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

    public async Task CreateTextIndex<T>(IRepository<T> repository, string[] fieldNames, string indexName,
        CancellationToken cancellationToken = default) where T : BaseEntity
    {
        ArgumentNullException.ThrowIfNullOrEmpty(indexName);
        ArgumentNullException.ThrowIfNull(fieldNames);

        //no stemming or stop words - product names are in many languages
        var model = new CreateIndexModel<T>(
            Builders<T>.IndexKeys.Combine(fieldNames.Select(x => Builders<T>.IndexKeys.Text(x))),
            new CreateIndexOptions { Name = indexName, DefaultLanguage = "none" });
        var indexes = ((MongoRepository<T>)repository).Collection.Indexes;
        try
        {
            await indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
        }
        catch (MongoCommandException ex) when (ex.Code is IndexOptionsConflict or IndexKeySpecsConflict)
        {
            //the fields changed since the index was built - rebuild it. A text index of another name still fails
            await DeleteIndex(repository, indexName);
            await indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
        }
    }

    public async Task<bool> TextIndexExists<T>(IRepository<T> repository, string indexName) where T : BaseEntity
    {
        ArgumentNullException.ThrowIfNullOrEmpty(indexName);

        var collection = ((MongoRepository<T>)repository).Collection;
        var indexes = await (await collection.Indexes.ListAsync()).ToListAsync();
        if (!indexes.Any(x => x["name"] == indexName))
            return false;
        try
        {
            //listIndexes reports an index still being built; $text fails until the build is done
            await collection.Find(Builders<T>.Filter.Text("probe")).Limit(1).AnyAsync();
            return true;
        }
        catch (MongoCommandException)
        {
            return false;
        }
    }

    public IQueryable<T> TextSearch<T>(IRepository<T> repository, string search) where T : BaseEntity
    {
        //the server accepts $text only in the first $match of a pipeline - the table is where it starts
        var filter = Builders<T>.Filter.Text(search);
        return repository.Table.Where(_ => filter.Inject());
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