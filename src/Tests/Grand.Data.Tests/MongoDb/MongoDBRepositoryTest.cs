using Grand.Domain;
using Grand.Domain.Data;
using Grand.Domain.Data.Mongo;

namespace Grand.Data.Tests.MongoDb
{
    public partial class MongoDBRepositoryTest<T> : MongoRepository<T>, IRepository<T> where T : BaseEntity
    {
        public MongoDBRepositoryTest():base(DriverTestConfiguration.Client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName))
        {
            var client = DriverTestConfiguration.Client;
            _database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            _database.DropCollection(typeof(T).Name);
            _collection = _database.GetCollection<T>(typeof(T).Name);
        }
    }
}
