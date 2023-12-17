﻿using Grand.Domain;
using Grand.Domain.Data;
using Grand.Domain.Data.LiteDb;
using LiteDB;

namespace Grand.Data.Tests.LiteDb
{
    public class LiteDBRepositoryMock<T> : LiteDBRepository<T>, IRepository<T> where T : BaseEntity
    {
        public LiteDBRepositoryMock(): base(Guid.NewGuid().ToString(), new AuditInfoProvider())
        {
            _database = new LiteDatabase(new MemoryStream());
            _database.DropCollection(typeof(T).Name);
            _collection = _database.GetCollection<T>(typeof(T).Name);
        }
    }
}
