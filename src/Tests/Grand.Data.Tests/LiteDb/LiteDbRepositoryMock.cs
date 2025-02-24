using Grand.Data.LiteDb;
using Grand.Domain;
using LiteDB;

namespace Grand.Data.Tests.LiteDb;

public class LiteDBRepositoryMock<T> : LiteDBRepository<T>, IRepository<T> where T : BaseEntity
{
    public LiteDBRepositoryMock() : base(Guid.NewGuid().ToString(), new AuditInfoProvider())
    {
        Database = new LiteDatabase(new MemoryStream());
        Database.DropCollection(typeof(T).Name);
        Collection = Database.GetCollection<T>(typeof(T).Name);
    }
}