using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Domain.Data
{
    public interface IMongoDBContext
    {
        IMongoDatabase Database();
        IQueryable<T> Table<T>(string collectionName);
        Task<byte[]> GridFSBucketDownload(string id);
        Task<string> GridFSBucketUploadFromBytesAsync(string filename, byte[] source);
        Task<bool> DatabaseExist(string connectionString);
        TResult RunCommand<TResult>(string command);
        BsonDocument RunCommand(string command);
        TResult RunCommand<TResult>(string command, ReadPreference readpreference);
        BsonValue RunScript(string command, CancellationToken cancellationToken);
        Task<BsonValue> RunScriptAsync(string command, CancellationToken cancellationToken);
        string ExecuteScript(string query);
        IEnumerable<Dictionary<string, object>> Serialize(List<BsonValue> collection);
    }
}
