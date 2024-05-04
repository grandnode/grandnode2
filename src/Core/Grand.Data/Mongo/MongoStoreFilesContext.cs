using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Grand.Data.Mongo;

public class MongoStoreFilesContext : IStoreFilesContext
{
    protected IMongoDatabase _database;

    public MongoStoreFilesContext()
    {
        var connectionString = DataSettingsManager.LoadSettings().ConnectionString;

        var mongourl = new MongoUrl(connectionString);
        var databaseName = mongourl.DatabaseName;
        _database = new MongoClient(connectionString).GetDatabase(databaseName);
    }

    public MongoStoreFilesContext(IMongoDatabase database)
    {
        _database = database;
    }

    public async Task<byte[]> BucketDownload(string id)
    {
        var bucket = new GridFSBucket(_database);
        var binary = await bucket.DownloadAsBytesAsync(new ObjectId(id), new GridFSDownloadOptions { CheckMD5 = true });
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