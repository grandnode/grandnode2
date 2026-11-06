using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Grand.Data.Tests;

/// <summary>
///     The behavior every <see cref="IResourceLock" /> shares, run against each database
/// </summary>
public abstract class ResourceLockTestsBase
{
    //keys of their own, so a test run elsewhere against the same database never meets them
    private readonly string _key = $"Test:{Guid.NewGuid():N}";

    protected abstract IResourceLock CreateLock(TimeSpan? lease = null, TimeSpan? wait = null);

    [TestMethod]
    public async Task Acquire_HeldKey_WaitsAndTimesOut()
    {
        var resourceLock = CreateLock(wait: TimeSpan.FromMilliseconds(200));

        await using var first = await resourceLock.Acquire([_key]);

        await Assert.ThrowsExactlyAsync<TimeoutException>(() => resourceLock.Acquire([_key]));
    }

    [TestMethod]
    public async Task Acquire_AfterDispose_Succeeds()
    {
        var resourceLock = CreateLock(wait: TimeSpan.FromMilliseconds(200));

        var first = await resourceLock.Acquire([_key]);
        await first.DisposeAsync();

        await using var second = await resourceLock.Acquire([_key]);
    }

    [TestMethod]
    public async Task Acquire_ExpiredLease_IsTakenOver()
    {
        var resourceLock = CreateLock(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200));

        //the holder never lets go, as if its process had died
        await resourceLock.Acquire([_key]);
        await Task.Delay(150);

        await using var second = await resourceLock.Acquire([_key]);
    }

    [TestMethod]
    public async Task Acquire_ExpiredLeaseTakenOver_FirstHolderDoesNotReleaseTheNewOne()
    {
        var shortLease = CreateLock(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200));
        var longLease = CreateLock(wait: TimeSpan.FromMilliseconds(200));

        var first = await shortLease.Acquire([_key]);
        await Task.Delay(150);
        await using var second = await longLease.Acquire([_key]);
        await first.DisposeAsync();

        await Assert.ThrowsExactlyAsync<TimeoutException>(() => longLease.Acquire([_key]));
    }

    [TestMethod]
    public async Task Acquire_DuplicateKeysInOneCall_DoesNotWaitOnItself()
    {
        var resourceLock = CreateLock(wait: TimeSpan.FromSeconds(5));
        var stopwatch = Stopwatch.StartNew();

        await using var handle = await resourceLock.Acquire([_key, _key]);

        Assert.IsLessThan(200, stopwatch.ElapsedMilliseconds);
    }

    [TestMethod]
    public async Task Acquire_ParallelHolders_RunOneAtATime()
    {
        var resourceLock = CreateLock();
        var counter = 0;

        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(async () =>
        {
            await using var handle = await resourceLock.Acquire([_key]);
            var read = counter;
            await Task.Yield();
            counter = read + 1;
        })));

        Assert.AreEqual(20, counter);
    }
}
