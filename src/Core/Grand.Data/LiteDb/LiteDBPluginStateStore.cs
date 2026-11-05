using LiteDB;

namespace Grand.Data.LiteDb;

/// <summary>
///     The plugin states in LiteDB, as plain documents. LiteDB locks its file, so once the application's one
///     instance exists (<see cref="UseShared" />) every call goes through it; before that - only while the
///     application starts - the file is opened for the call and closed again.
/// </summary>
public sealed class LiteDBPluginStateStore : IPluginStateStore
{
    private readonly Func<string> _connectionString;
    private readonly Lock _lock = new();
    private Func<LiteDatabase> _shared;

    /// <param name="connectionString">Read on every call: the installation sets it while the application runs</param>
    public LiteDBPluginStateStore(Func<string> connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        _connectionString = connectionString;
    }

    public bool HasDatabase => !string.IsNullOrEmpty(_connectionString());

    /// <summary>
    ///     Uses the application's instance from now on
    /// </summary>
    public void UseShared(Func<LiteDatabase> database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _shared = database;
    }

    public IList<string> InstalledSystemNames()
    {
        return Use(collection => collection.Find(Query.EQ(PluginStateDocument.Installed, true))
            .Select(x => x[PluginStateDocument.SystemName].AsString)
            .ToList());
    }

    public Task<IList<string>> InstalledSystemNamesAsync()
    {
        return Task.FromResult(InstalledSystemNames());
    }

    public bool ImportInstalled(Func<IEnumerable<string>> systemNames)
    {
        ArgumentNullException.ThrowIfNull(systemNames);

        return Use(collection =>
        {
            if (collection.Count() > 0)
                return false;

            foreach (var systemName in systemNames().DistinctBy(PluginStateDocument.IdOf))
                collection.Upsert(new BsonDocument {
                    [PluginStateDocument.Id] = PluginStateDocument.IdOf(systemName),
                    [PluginStateDocument.SystemName] = systemName,
                    [PluginStateDocument.Installed] = true
                });

            return true;
        });
    }

    public Task SetInstalled(string systemName, bool installed)
    {
        Use(collection =>
        {
            var document = Find(collection, systemName);
            document[PluginStateDocument.Installed] = installed;
            return collection.Upsert(document);
        });
        return Task.CompletedTask;
    }

    public Task Clear()
    {
        Use(collection => collection.DeleteAll());
        return Task.CompletedTask;
    }

    public void SaveLoadResults(IEnumerable<PluginState> plugins)
    {
        ArgumentNullException.ThrowIfNull(plugins);

        Use(collection =>
        {
            foreach (var plugin in plugins.DistinctBy(x => PluginStateDocument.IdOf(x.SystemName)))
            {
                var document = Find(collection, plugin.SystemName);
                document[PluginStateDocument.SystemName] = plugin.SystemName;
                document[PluginStateDocument.FriendlyName] = plugin.FriendlyName;
                document[PluginStateDocument.Version] = plugin.Version;
                document[PluginStateDocument.FolderName] = plugin.FolderName;
                document[PluginStateDocument.LoadError] = plugin.LoadError;
                document[PluginStateDocument.LoadedOnUtc] = plugin.LoadedOnUtc;
                collection.Upsert(document);
            }

            return true;
        });
    }

    /// <summary>
    ///     The plugin's document, or a new one of a plugin that is not installed
    /// </summary>
    private static BsonDocument Find(ILiteCollection<BsonDocument> collection, string systemName)
    {
        var id = PluginStateDocument.IdOf(systemName);
        return collection.FindById(id) ?? new BsonDocument {
            [PluginStateDocument.Id] = id,
            [PluginStateDocument.SystemName] = systemName,
            [PluginStateDocument.Installed] = false
        };
    }

    /// <summary>
    ///     Runs one call at a time: a read and its write must not interleave with another's
    /// </summary>
    private T Use<T>(Func<ILiteCollection<BsonDocument>, T> action)
    {
        var connectionString = _connectionString();
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("The database is not installed yet");

        lock (_lock)
        {
            if (_shared != null)
                return action(_shared().GetCollection(PluginStateDocument.Collection));

            using var database = new LiteDatabase(connectionString);
            return action(database.GetCollection(PluginStateDocument.Collection));
        }
    }
}
