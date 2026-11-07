using Grand.Data.Mongo;

namespace Grand.Data.Tests.MongoDb;

public class MongoResourceLockTest(TimeSpan? lease = null, TimeSpan? wait = null) : MongoResourceLock(
    () => DriverTestConfiguration.Client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName),
    lease, wait);
