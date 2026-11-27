using Grand.Web.Common.Startup;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Common.Tests.Startup;

[TestClass]
public class RateLimitPartitionTests
{
    [TestMethod]
    public void ConfiguredPermits_RefusesTheNextOne()
    {
        using var limiter = GrandCommonStartup.FixedWindow("key", 2, 10).Factory("key");

        Assert.IsTrue(limiter.AttemptAcquire().IsAcquired);
        Assert.IsTrue(limiter.AttemptAcquire().IsAcquired);
        Assert.IsFalse(limiter.AttemptAcquire().IsAcquired);
    }

    //the limiter refuses a zero limit or window when it is created, at the first request
    [TestMethod]
    [DataRow(0, 10)]
    [DataRow(5, 0)]
    public void Zero_NoLimit(int permits, int windowMinutes)
    {
        using var limiter = GrandCommonStartup.FixedWindow("key", permits, windowMinutes).Factory("key");

        for (var i = 0; i < 100; i++)
            Assert.IsTrue(limiter.AttemptAcquire().IsAcquired);
    }
}