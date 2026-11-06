using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Data.Tests.MongoDb;

[TestClass]
public class MongoResourceLockTests : ResourceLockTestsBase
{
    protected override IResourceLock CreateLock(TimeSpan? lease = null, TimeSpan? wait = null)
    {
        return new MongoResourceLockTest(lease, wait);
    }
}
