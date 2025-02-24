using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Grand.Data.Mongo;

public class MongoStoreFilesContext : IStoreFilesContext
{
    private readonly IMongoDatabase _database;

    public MongoStoreFilesContext()
    {
        var connectionString = DataSettingsManager.Instance.LoadSettings().ConnectionString;

        var mongoUrl = new MongoUrl(connectionString);
        var databaseName = mongoUrl.DatabaseName;
        _database = new MongoClient(connectionString).GetDatabase(databaseName);
    }

    public MongoStoreFilesContext(IMongoDatabase database)
    {
        _database = database;
    }

    public async Task<byte[]> BucketDownload(string id)
    {
        var bucket = new GridFSBucket(_database);
        var binary = await bucket.DownloadAsBytesAsync(new ObjectId(id), new GridFSDownloadOptions());
        return binary;
    }

    public async Task BucketDelete(string id)
    {
        var bucket = new GridFSBucket(_database);
        await bucket.DeleteAsync(new ObjectId(id));
    }

    public async Task<string> BucketUploadFromBytes(string filename, byte[] source)
    {
        var bucket = new GridFSBucket(_database);
        var id = await bucket.UploadFromBytesAsync(filename, source);
        return id.ToString();
    }
}