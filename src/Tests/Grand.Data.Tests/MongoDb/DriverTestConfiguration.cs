using MongoDB.Driver;

namespace Grand.Data.Tests.MongoDb;

/// <summary>
///     A static class to handle online test configuration.
/// </summary>
public static class DriverTestConfiguration
{
    // private static fields
    private static readonly Lazy<MongoClient> __client;

    // static constructor
    static DriverTestConfiguration()
    {
        var connectionString = CoreTestConfiguration.ConnectionString.ToString();
        var clientSettings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));

        var serverSelectionTimeoutString = Environment.GetEnvironmentVariable("MONGO_SERVER_SELECTION_TIMEOUT_MS");
        if (serverSelectionTimeoutString == null) serverSelectionTimeoutString = "30000";
        clientSettings.ServerSelectionTimeout = TimeSpan.FromMilliseconds(int.Parse(serverSelectionTimeoutString));
        clientSettings.ClusterConfigurator = cb => CoreTestConfiguration.ConfigureLogging(cb);

        __client = new Lazy<MongoClient>(() => new MongoClient(clientSettings), true);
        DatabaseNamespace = CoreTestConfiguration.DatabaseNamespace;
        CollectionNamespace = new CollectionNamespace(DatabaseNamespace, "testcollection");
    }

    // public static properties
    /// <summary>
    ///     Gets the test client.
    /// </summary>
    public static MongoClient Client => __client.Value;

    /// <summary>
    ///     Gets the collection namespace.
    /// </summary>
    /// <value>
    ///     The collection namespace.
    /// </value>
    public static CollectionNamespace CollectionNamespace { get; }

    /// <summary>
    ///     Gets the database namespace.
    /// </summary>
    /// <value>
    ///     The database namespace.
    /// </value>
    public static DatabaseNamespace DatabaseNamespace { get; }
}