using Grand.Domain;
using Grand.Domain.Data;
using Grand.Domain.Data.LiteDb;
using LiteDB;

namespace Grand.Data.Tests.LiteDb
{
    public partial class MongoDBRepositoryMock<T> : LiteDBRepository<T>, IRepository<T> where T : BaseEntity
    {
        public MongoDBRepositoryMock(): base("MemoryStream")
        {
            _database = new LiteDatabase(new MemoryStream());
            _database.DropCollection(typeof(T).Name);
            _collection = _database.GetCollection<T>(typeof(T).Name);
        }
    }
}
