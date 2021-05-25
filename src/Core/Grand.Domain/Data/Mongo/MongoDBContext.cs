using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Domain.Data.Mongo
{
    public class MongoDBContext : IDatabaseContext
    {
        protected IMongoDatabase _database;

        public MongoDBContext()
        {

        }

        public MongoDBContext(IMongoDatabase mongodatabase)
        {
            _database = mongodatabase;
        }

        public IMongoDatabase Database()
        {
            return _database;
        }

        public IQueryable<T> Table<T>(string collectionName)
        {
            return _database.GetCollection<T>(collectionName).AsQueryable();
        }

        protected IMongoDatabase TryReadMongoDatabase()
        {
            var connectionString = DataSettingsManager.LoadSettings().ConnectionString;

            var mongourl = new MongoUrl(connectionString);
            var databaseName = mongourl.DatabaseName;
            var mongodb = new MongoClient(connectionString).GetDatabase(databaseName);
            return mongodb;
        }

        public async Task<byte[]> GridFSBucketDownload(string id)
        {
            var bucket = new MongoDB.Driver.GridFS.GridFSBucket(_database);
            var binary = await bucket.DownloadAsBytesAsync(new ObjectId(id), new MongoDB.Driver.GridFS.GridFSDownloadOptions() { CheckMD5 = true, Seekable = true });
            return binary;

        }
        public async Task<string> GridFSBucketUploadFromBytesAsync(string filename, byte[] source)
        {
            var database = _database ?? TryReadMongoDatabase();
            var bucket = new MongoDB.Driver.GridFS.GridFSBucket(database);
            var id = await bucket.UploadFromBytesAsync(filename, source);
            return id.ToString();
        }

        public async Task<bool> DatabaseExist(string connectionString)
        {
            var client = new MongoClient(connectionString);
            var databaseName = new MongoUrl(connectionString).DatabaseName;
            var database = client.GetDatabase(databaseName);
            await database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");

            var filter = new BsonDocument("name", "GrandNodeVersion");
            var found = database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter }).Result;
            if (found.Any())
                return true;
            else
                return false;
        }
    }
}
