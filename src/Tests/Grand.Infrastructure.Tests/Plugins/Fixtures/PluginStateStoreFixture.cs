using Grand.Data;
using Grand.Data.LiteDb;
using Grand.Infrastructure.Plugins;
using LiteDB;

namespace Grand.Infrastructure.Tests.Plugins.Fixtures;

/// <summary>
///     The LiteDB plugin state store on a database in memory, set as <see cref="PluginManager.StateStore" /> until
///     disposed. Tests using it change a static, so they do not run in parallel.
/// </summary>
public sealed class PluginStateStoreFixture : IDisposable
{
    private readonly LiteDatabase _database = new(new MemoryStream());
    private readonly IPluginStateStore _previous = PluginManager.StateStore;

    public PluginStateStoreFixture()
    {
        Store = new LiteDBPluginStateStore(() => "Filename=:memory:");
        Store.UseShared(() => _database);
        PluginManager.StateStore = Store;
    }

    public LiteDBPluginStateStore Store { get; }

    public List<string> Installed()
    {
        return Store.InstalledSystemNames().ToList();
    }

    /// <summary>
    ///     The stored document of the plugin; null when the store does not list it
    /// </summary>
    public BsonDocument Document(string systemName)
    {
        return _database.GetCollection("PluginState").FindById(systemName.ToLowerInvariant());
    }

    public void Dispose()
    {
        PluginManager.StateStore = _previous;
        _database.Dispose();
    }
}
