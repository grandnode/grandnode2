using Grand.Data.Mongo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Data.Tests.MongoDb;

[TestClass]
public class MongoResourceLockTests : ResourceLockTestsBase
{
    protected override IResourceLock CreateLock(TimeSpan? lease = null, TimeSpan? wait = null)
    {
        return new MongoResourceLock(
            () => DriverTestConfiguration.Client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName),
            lease, wait);
    }
}
