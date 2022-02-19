using LiteDB;

namespace Grand.Domain.Data.LiteDb
{
    public class LiteDBContext : IDatabaseContext
    {
        private string _connectionString;
        protected LiteDatabase _database;

        public LiteDBContext()
        {
            var connection = DataSettingsManager.LoadSettings();
            if (!string.IsNullOrEmpty(connection.ConnectionString))
                PrepareMongoDatabase(connection.ConnectionString);
        }

        private void PrepareMongoDatabase(string connectionString)
        {
            _connectionString = connectionString;
            _database = new LiteDatabase(connectionString);
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

            return _database.GetCollection<T>(collectionName).FindAll().AsQueryable();
        }

        public async Task<bool> DatabaseExist()
        {
            if (string.IsNullOrEmpty(_connectionString))
                throw new ArgumentNullException(nameof(_connectionString));

            return await Task.FromResult(_database.CollectionExists(nameof(Grand.Domain.Common.GrandNodeVersion)));
        }

        public Task CreateTable(string name, string collation)
        {
            //Not supported by LiteDB
            return Task.CompletedTask;
        }

        public Task DeleteTable(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            _database.DropCollection(name);

            return Task.CompletedTask;
        }

        public Task CreateIndex<T>(IRepository<T> repository, OrderBuilder<T> orderBuilder, string indexName, bool unique = false) where T : BaseEntity
        {
            if (string.IsNullOrEmpty(indexName))
                throw new ArgumentNullException(nameof(indexName));

            try
            {
                foreach (var (selector, value, fieldName) in orderBuilder.Fields)
                {
                    _database.GetCollection<T>().EnsureIndex(selector, unique);
                }
            }
            catch { }

            return Task.CompletedTask;
        }

        public Task DeleteIndex<T>(IRepository<T> repository, string indexName) where T : BaseEntity
        {

            if (string.IsNullOrEmpty(indexName))
                throw new ArgumentNullException(nameof(indexName));
            try
            {
                _database.GetCollection<T>().DropIndex(indexName);
            }
            catch { }

            return Task.CompletedTask;
        }
    }
}
