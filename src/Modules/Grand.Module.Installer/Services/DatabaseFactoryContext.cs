using Grand.Data;
using Grand.Data.LiteDb;
using Grand.Data.Mongo;
using Grand.Module.Installer.Interfaces;
using MongoDB.Driver;

namespace Grand.Module.Installer.Services;

public class DatabaseFactoryContext : IDatabaseFactoryContext
{
    public IDatabaseContext GetDatabaseContext(string? connectionString = null, DbProvider? dbProvider = null)
    {
        var dataSettings = string.IsNullOrEmpty(connectionString) ? DataSettingsManager.Instance.LoadSettings(true) : 
            new DataSettings() { ConnectionString = connectionString, DbProvider = dbProvider.HasValue ? dbProvider.Value : DbProvider.MongoDB };
        if (dataSettings != null && !string.IsNullOrEmpty(dataSettings.ConnectionString))
        {
            if (dataSettings.DbProvider != DbProvider.LiteDB)
            {
                var databaseName = new MongoUrl(dataSettings.ConnectionString).DatabaseName;
                var database = new MongoClient(dataSettings.ConnectionString).GetDatabase(databaseName);
                return new MongoDBContext(database);
            }
            else
            {
                var dbContext = new LiteDB.LiteDatabase(dataSettings.ConnectionString);
                return new LiteDBContext(dbContext);
            }
        }
        throw new Exception("No database provider was found");
    }
}
