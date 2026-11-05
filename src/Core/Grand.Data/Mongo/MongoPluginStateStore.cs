using MongoDB.Bson;
using MongoDB.Driver;

namespace Grand.Data.Mongo;

/// <summary>
///     The plugin states in MongoDB, as plain documents: the installed list is read before the conventions and class
///     maps are registered, and mapping a class then would freeze its map without them. Until the application's
///     client exists (<see cref="UseShared" />) - while the application starts, or is being installed - the store
///     uses a client of its own.
/// </summary>
public sealed class MongoPluginStateStore : IPluginStateStore
{
    private static readonly UpdateOptions Upsert = new() { IsUpsert = true };

    private readonly Func<string> _connectionString;
    private readonly Lock _lock = new();
    private Func<IMongoClient> _shared;
    private MongoClient _ownClient;
    private string _currentConnectionString;
    private IMongoCollection<BsonDocument> _collection;

    /// <param name="connectionString">Read on every call: the installation sets it while the application runs</param>
    public MongoPluginStateStore(Func<string> connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        _connectionString = connectionString;
    }

    public bool HasDatabase => !string.IsNullOrEmpty(_connectionString());

    /// <summary>
    ///     Uses the application's client from now on, and lets go of the store's own
    /// </summary>
    public void UseShared(Func<IMongoClient> client)
    {
        ArgumentNullException.ThrowIfNull(client);

        lock (_lock)
        {
            _shared = client;
            _collection = null;
            _ownClient?.Dispose();
            _ownClient = null;
        }
    }

    public IList<string> InstalledSystemNames()
    {
        return Collection().Find(InstalledFilter()).ToList()
            .Select(x => x[PluginStateDocument.SystemName].AsString)
            .ToList();
    }

    public async Task<IList<string>> InstalledSystemNamesAsync()
    {
        var documents = await Collection().Find(InstalledFilter()).ToListAsync();
        return documents.Select(x => x[PluginStateDocument.SystemName].AsString).ToList();
    }

    public bool ImportInstalled(Func<IEnumerable<string>> systemNames)
    {
        ArgumentNullException.ThrowIfNull(systemNames);

        var collection = Collection();
        if (collection.Find(FilterDefinition<BsonDocument>.Empty).Limit(1).Any())
            return false;

        foreach (var systemName in systemNames().DistinctBy(PluginStateDocument.IdOf))
            collection.UpdateOne(ById(systemName), InstalledUpdate(systemName, true), Upsert);

        return true;
    }

    public Task SetInstalled(string systemName, bool installed)
    {
        return Collection().UpdateOneAsync(ById(systemName), InstalledUpdate(systemName, installed), Upsert);
    }

    public Task Clear()
    {
        return Collection().DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
    }

    public void SaveLoadResults(IEnumerable<PluginState> plugins)
    {
        ArgumentNullException.ThrowIfNull(plugins);

        var writes = plugins.DistinctBy(x => PluginStateDocument.IdOf(x.SystemName))
            .Select(x => new UpdateOneModel<BsonDocument>(ById(x.SystemName), new BsonDocument {
                {
                    "$set", new BsonDocument {
                        { PluginStateDocument.SystemName, x.SystemName },
                        { PluginStateDocument.FriendlyName, ValueOf(x.FriendlyName) },
                        { PluginStateDocument.Version, ValueOf(x.Version) },
                        { PluginStateDocument.FolderName, ValueOf(x.FolderName) },
                        { PluginStateDocument.LoadError, ValueOf(x.LoadError) },
                        { PluginStateDocument.LoadedOnUtc, x.LoadedOnUtc }
                    }
                },
                { "$setOnInsert", new BsonDocument(PluginStateDocument.Installed, false) }
            }) { IsUpsert = true })
            .ToList();

        if (writes.Count > 0)
            Collection().BulkWrite(writes);
    }

    private static BsonValue ValueOf(string value)
    {
        return value == null ? BsonNull.Value : new BsonString(value);
    }

    private static FilterDefinition<BsonDocument> InstalledFilter()
    {
        return new BsonDocument(PluginStateDocument.Installed, true);
    }

    private static FilterDefinition<BsonDocument> ById(string systemName)
    {
        return new BsonDocument(PluginStateDocument.Id, PluginStateDocument.IdOf(systemName));
    }

    private static UpdateDefinition<BsonDocument> InstalledUpdate(string systemName, bool installed)
    {
        return new BsonDocument {
            { "$set", new BsonDocument(PluginStateDocument.Installed, installed) },
            { "$setOnInsert", new BsonDocument(PluginStateDocument.SystemName, systemName) }
        };
    }

    private IMongoCollection<BsonDocument> Collection()
    {
        var connectionString = _connectionString();
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("The database is not installed yet");

        lock (_lock)
        {
            if (_collection == null || _currentConnectionString != connectionString)
            {
                IMongoClient client;
                if (_shared != null)
                    client = _shared();
                else
                {
                    //the installation sets the connection string while the application runs
                    _ownClient?.Dispose();
                    client = _ownClient = new MongoClient(connectionString);
                }

                _collection = client.GetDatabase(new MongoUrl(connectionString).DatabaseName)
                    .GetCollection<BsonDocument>(PluginStateDocument.Collection);
                _currentConnectionString = connectionString;
            }

            return _collection;
        }
    }
}
