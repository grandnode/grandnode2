using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Operations;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Domain.Data
{
    public class MongoDBContext : IMongoDBContext
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

        public TResult RunCommand<TResult>(string command)
        {
            return _database.RunCommand<TResult>(command);
        }

        public BsonDocument RunCommand(string command)
        {
            return _database.RunCommand<BsonDocument>(command);
        }

        public TResult RunCommand<TResult>(string command, ReadPreference readpreference)
        {
            return _database.RunCommand<TResult>(command, readpreference);
        }

        public BsonValue RunScript(string command, CancellationToken cancellationToken)
        {
            var script = new BsonJavaScript(command);
            var operation = new EvalOperation(_database.DatabaseNamespace, script, null);
            var writeBinding = new WritableServerBinding(_database.Client.Cluster, NoCoreSession.NewHandle());
            return operation.Execute(writeBinding, CancellationToken.None);
        }

        public Task<BsonValue> RunScriptAsync(string command, CancellationToken cancellationToken)
        {
            var script = new BsonJavaScript(command);
            var operation = new EvalOperation(_database.DatabaseNamespace, script, null);
            var writeBinding = new WritableServerBinding(_database.Client.Cluster, NoCoreSession.NewHandle());
            return operation.ExecuteAsync(writeBinding, CancellationToken.None);
        }

        public string ExecuteScript(string query)
        {
            var bscript = new BsonJavaScript(query);
            var operation = new EvalOperation(Database().DatabaseNamespace, bscript, null);
            var writeBinding = new WritableServerBinding(Database().Client.Cluster, NoCoreSession.NewHandle());
            var result = operation.Execute(writeBinding, CancellationToken.None);
            return result["_ns"].ToString();
        }

        public virtual IEnumerable<Dictionary<string, object>> Serialize(List<BsonValue> collection)
        {
            var results = new List<Dictionary<string, object>>();
            var columns = new List<string>();
            var document = collection.FirstOrDefault()?.AsBsonDocument;
            if (document != null)
            {
                foreach (var item in document.Names)
                {
                    columns.Add(item);
                }
                foreach (var row in collection)
                {
                    var myObject = new Dictionary<string, object>();
                    foreach (var col in columns)
                    {
                        myObject.Add(col, row[col].ToString());
                    }
                    results.Add(myObject);
                }
            }
            return results;
        }
    }
}
