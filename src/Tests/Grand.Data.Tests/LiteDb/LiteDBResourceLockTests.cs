using Grand.Data.LiteDb;
using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Data.Tests.LiteDb;

[TestClass]
public class LiteDBResourceLockTests : ResourceLockTestsBase
{
    private LiteDatabase _database;

    [TestInitialize]
    public void Init()
    {
        _database = new LiteDatabase(new MemoryStream());
    }

    [TestCleanup]
    public void Cleanup() => _database.Dispose();

    protected override IResourceLock CreateLock(TimeSpan? lease = null, TimeSpan? wait = null)
    {
        return new LiteDBResourceLock(() => _database, lease, wait);
    }
}
