using MongoDB.Bson;
using MongoDB.Driver;

namespace Grand.Domain.Data.Mongo
{
    public class MongoDBContext : IDatabaseContext
    {
        private string _connectionString;
        protected IMongoDatabase _database;

        public MongoDBContext()
        {
            var connection = DataSettingsManager.LoadSettings();
            if (!string.IsNullOrEmpty(connection.ConnectionString))
                PrepareMongoDatabase(connection.ConnectionString);
        }
       
        private void PrepareMongoDatabase(string connectionString)
        {
            _connectionString = connectionString;
            var mongourl = new MongoUrl(connectionString);
            var databaseName = mongourl.DatabaseName;
            _database = new MongoClient(connectionString).GetDatabase(databaseName);
        }

        
        public MongoDBContext(IMongoDatabase mongodatabase)
        {
            _database = mongodatabase;
        }

        public IMongoDatabase Database()
        {
            return _database;
        }

        public void SetConnection(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            PrepareMongoDatabase(connectionString);
        }

        public bool InstallProcessCreateTable => true;
        public bool InstallProcessCreateIndex => true;

        public IQueryable<T> Table<T>(string collectionName)
        {
            if (string.IsNullOrEmpty(collectionName))
                throw new ArgumentNullException(nameof(collectionName));

            return _database.GetCollection<T>(collectionName).AsQueryable();
        }

        protected IMongoDatabase TryReadMongoDatabase()
        {
            _connectionString = DataSettingsManager.LoadSettings().ConnectionString;

            var mongourl = new MongoUrl(_connectionString);
            var databaseName = mongourl.DatabaseName;
            var mongodb = new MongoClient(_connectionString).GetDatabase(databaseName);
            return mongodb;
        }

        public async Task<bool> DatabaseExist()
        {
            if (string.IsNullOrEmpty(_connectionString))
                throw new ArgumentNullException(nameof(_connectionString));

            var client = new MongoClient(_connectionString);
            var databaseName = new MongoUrl(_connectionString).DatabaseName;
            var database = client.GetDatabase(databaseName);
            await database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");

            var filter = new BsonDocument("name", "GrandNodeVersion");
            var found = database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter }).Result;
            if (found.Any())
                return true;
            else
                return false;
        }

        public async Task CreateTable(string name, string collation)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (!string.IsNullOrEmpty(collation))
            {
                var options = new CreateCollectionOptions();
                options.Collation = new Collation(collation);
                await _database.CreateCollectionAsync(name, options);
            }
            else
                await _database.CreateCollectionAsync(name);

        }

        public async Task DeleteTable(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            await _database.DropCollectionAsync(name);
        }

        public async Task CreateIndex<T>(IRepository<T> repository, OrderBuilder<T> orderBuilder, string indexName, bool unique = false) where T : BaseEntity
        {
            if (string.IsNullOrEmpty(indexName))
                throw new ArgumentNullException(nameof(indexName));

            IList<IndexKeysDefinition<T>> keys = new List<IndexKeysDefinition<T>>();
            foreach (var item in orderBuilder.Fields)
            {
                if (item.selector != null)
                {
                    if (item.value)
                    {
                        keys.Add(Builders<T>.IndexKeys.Ascending(item.selector));
                    }
                    else
                    {
                        keys.Add(Builders<T>.IndexKeys.Descending(item.selector));
                    }
                }
                else
                {
                    if (item.value)
                    {
                        keys.Add(Builders<T>.IndexKeys.Ascending(item.fieldName));
                    }
                    else
                    {
                        keys.Add(Builders<T>.IndexKeys.Descending(item.fieldName));
                    }
                }
            }

            try
            {
                await ((MongoRepository<T>)repository).Collection.Indexes.CreateOneAsync(new CreateIndexModel<T>(Builders<T>.IndexKeys.Combine(keys),
                    new CreateIndexOptions() { Name = indexName, Unique = unique }));
            }
            catch { }
        }

        public async Task DeleteIndex<T>(IRepository<T> repository, string indexName) where T : BaseEntity
        {
            if (string.IsNullOrEmpty(indexName))
                throw new ArgumentNullException(nameof(indexName));
            try
            {
                await ((MongoRepository<T>)repository).Collection.Indexes.DropOneAsync(indexName);
            }
            catch { }
        }
    }
}
