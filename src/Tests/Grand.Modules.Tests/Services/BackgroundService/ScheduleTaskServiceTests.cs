using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Data.Tests.LiteDb;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Tasks;
using Grand.Module.ScheduledTasks.BackgroundServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Services.BackgroundService;

[TestClass]
public class ScheduleTaskServiceTests
{
    private MongoDBRepositoryTest<ScheduleTask> _repository;
    private ScheduleTaskService _service;

    private static readonly DateTime LeasedUntilUtc = DateTime.UtcNow.AddMinutes(5);

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<ScheduleTask>();
        _service = new ScheduleTaskService(_repository);
    }

    private async Task<ScheduleTask> InsertTask(DateTime? lastStartUtc = null, DateTime? leasedUntilUtc = null,
        string leasedByInstance = null)
    {
        var task = new ScheduleTask {
            ScheduleTaskName = "TestTask",
            Enabled = true,
            TimeInterval = 1,
            LastStartUtc = lastStartUtc,
            LeasedUntilUtc = leasedUntilUtc,
            LeasedByInstance = leasedByInstance
        };
        await _service.InsertTask(task);
        //read back so LastStartUtc has the value as stored in the database (ms precision)
        return await _service.GetTaskById(task.Id);
    }

    [TestMethod]
    public async Task TryClaimTaskRun_FirstClaim_ReturnsTrueAndSetsLease()
    {
        var task = await InsertTask();
        var runStartUtc = DateTime.UtcNow;

        var claimed = await _service.TryClaimTaskRun(task.Id, task.LastStartUtc, runStartUtc, LeasedUntilUtc, "instance-a");

        Assert.IsTrue(claimed);
        var updated = await _service.GetTaskById(task.Id);
        Assert.AreEqual("instance-a", updated.LeasedByInstance);
        Assert.IsNotNull(updated.LastStartUtc);
    }

    [TestMethod]
    public async Task TryClaimTaskRun_StaleExpectedValue_ReturnsFalseAndKeepsWinner()
    {
        var task = await InsertTask(DateTime.UtcNow.AddMinutes(-10));

        //instance A claims the run
        var claimedByA = await _service.TryClaimTaskRun(task.Id, task.LastStartUtc, DateTime.UtcNow, LeasedUntilUtc, "instance-a");
        //instance B still holds the old LastStartUtc value - its claim must fail
        var claimedByB = await _service.TryClaimTaskRun(task.Id, task.LastStartUtc, DateTime.UtcNow, LeasedUntilUtc, "instance-b");

        Assert.IsTrue(claimedByA);
        Assert.IsFalse(claimedByB);
        var updated = await _service.GetTaskById(task.Id);
        Assert.AreEqual("instance-a", updated.LeasedByInstance);
    }

    [TestMethod]
    public async Task TryClaimTaskRun_ConcurrentClaims_ExactlyOneWins()
    {
        var task = await InsertTask(DateTime.UtcNow.AddMinutes(-10));

        var claims = await Task.WhenAll(
            _service.TryClaimTaskRun(task.Id, task.LastStartUtc, DateTime.UtcNow, LeasedUntilUtc, "instance-1"),
            _service.TryClaimTaskRun(task.Id, task.LastStartUtc, DateTime.UtcNow, LeasedUntilUtc, "instance-2"));

        Assert.AreEqual(1, claims.Count(x => x));
    }

    [TestMethod]
    public async Task TryClaimTaskRun_NonExistingTask_ReturnsFalse()
    {
        var claimed = await _service.TryClaimTaskRun("000000000000000000000000", null, DateTime.UtcNow, LeasedUntilUtc, "instance-a");

        Assert.IsFalse(claimed);
    }

    [TestMethod]
    public async Task TryClaimTaskRun_PreviousRunStillLeased_ReturnsFalseAndKeepsRunningInstance()
    {
        //a long run on instance A outlasts the interval - its lease is still in the future
        var task = await InsertTask(DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow.AddMinutes(3), "instance-a");

        var claimed = await _service.TryClaimTaskRun(task.Id, task.LastStartUtc, DateTime.UtcNow, LeasedUntilUtc,
            "instance-b");

        Assert.IsFalse(claimed);
        var updated = await _service.GetTaskById(task.Id);
        Assert.AreEqual("instance-a", updated.LeasedByInstance);
    }

    [TestMethod]
    public async Task TryClaimTaskRun_PreviousLeaseRanOut_ReturnsTrue()
    {
        //instance A died during the run and stopped renewing its lease
        var task = await InsertTask(DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow.AddMinutes(-4), "instance-a");

        var claimed = await _service.TryClaimTaskRun(task.Id, task.LastStartUtc, DateTime.UtcNow, LeasedUntilUtc,
            "instance-b");

        Assert.IsTrue(claimed);
        var updated = await _service.GetTaskById(task.Id);
        Assert.AreEqual("instance-b", updated.LeasedByInstance);
        Assert.IsNotNull(updated.LeasedUntilUtc);
    }

    [TestMethod]
    public async Task RenewTaskLease_OwnLease_ExtendsIt()
    {
        var task = await InsertTask(DateTime.UtcNow, DateTime.UtcNow.AddMinutes(1), "instance-a");

        var renewed = await _service.RenewTaskLease(task.Id, "instance-a", DateTime.UtcNow.AddMinutes(5));

        Assert.IsTrue(renewed);
        var updated = await _service.GetTaskById(task.Id);
        Assert.IsTrue(updated.LeasedUntilUtc > DateTime.UtcNow.AddMinutes(4));
    }

    [TestMethod]
    public async Task RenewTaskLease_LeaseTakenOverByAnotherInstance_DoesNothing()
    {
        var task = await InsertTask(DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5), "instance-b");

        var renewed = await _service.RenewTaskLease(task.Id, "instance-a", DateTime.UtcNow.AddMinutes(30));

        //the run must learn it lost the lease - it stops instead of running beside the new holder
        Assert.IsFalse(renewed);
        var updated = await _service.GetTaskById(task.Id);
        Assert.AreEqual("instance-b", updated.LeasedByInstance);
        Assert.IsTrue(updated.LeasedUntilUtc < DateTime.UtcNow.AddMinutes(6));
    }

    [TestMethod]
    public async Task RenewTaskLease_LeaseReleased_DoesNotRecreateIt()
    {
        //the run has finished and released the lease before a late renewal arrived
        var task = await InsertTask(DateTime.UtcNow, null, "instance-a");

        var renewed = await _service.RenewTaskLease(task.Id, "instance-a", DateTime.UtcNow.AddMinutes(5));

        Assert.IsFalse(renewed);
        var updated = await _service.GetTaskById(task.Id);
        Assert.IsNull(updated.LeasedUntilUtc);
    }

    [TestMethod]
    public async Task CompleteTaskRun_Succeeded_RecordsSuccessAndReleasesLease()
    {
        var task = await InsertTask(DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5), "instance-a");

        await _service.CompleteTaskRun(task.Id, "instance-a", true, DateTime.UtcNow, false);

        var updated = await _service.GetTaskById(task.Id);
        Assert.IsNull(updated.LeasedUntilUtc);
        Assert.IsNotNull(updated.LastSuccessUtc);
        Assert.IsNull(updated.LastNonSuccessEndUtc);
        Assert.IsTrue(updated.Enabled);
    }

    [TestMethod]
    public async Task CompleteTaskRun_FailedAndDisable_RecordsFailureAndDisablesTask()
    {
        var task = await InsertTask(DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5), "instance-a");

        await _service.CompleteTaskRun(task.Id, "instance-a", false, DateTime.UtcNow, true);

        var updated = await _service.GetTaskById(task.Id);
        Assert.IsNull(updated.LeasedUntilUtc);
        Assert.IsNull(updated.LastSuccessUtc);
        Assert.IsNotNull(updated.LastNonSuccessEndUtc);
        Assert.IsFalse(updated.Enabled);
    }

    [TestMethod]
    public async Task CompleteTaskRun_LeaseTakenOverByAnotherInstance_LeavesItsClaimAlone()
    {
        //instance A's lease ran out during a long run and instance B claimed the task
        var task = await InsertTask(DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5), "instance-b");

        await _service.CompleteTaskRun(task.Id, "instance-a", true, DateTime.UtcNow, false);

        var updated = await _service.GetTaskById(task.Id);
        Assert.AreEqual("instance-b", updated.LeasedByInstance);
        Assert.IsNotNull(updated.LeasedUntilUtc);
        Assert.IsNull(updated.LastSuccessUtc);
    }

    [TestMethod]
    public async Task CompleteTaskRun_KeepsChangesSavedDuringTheRun()
    {
        var task = await InsertTask(DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5), "instance-a");
        //the admin disables the task and changes its interval while it runs
        task.Enabled = false;
        task.TimeInterval = 30;
        await _service.UpdateTask(task);

        await _service.CompleteTaskRun(task.Id, "instance-a", true, DateTime.UtcNow, false);

        var updated = await _service.GetTaskById(task.Id);
        Assert.IsFalse(updated.Enabled);
        Assert.AreEqual(30, updated.TimeInterval);
    }

    [TestMethod]
    public async Task UpdateTask_EntityReadBeforeAClaim_KeepsTheLeaseOfTheRunningInstance()
    {
        //the admin opens the task, then instance A claims a run before the admin saves
        var task = await InsertTask(DateTime.UtcNow.AddMinutes(-10));
        await _service.TryClaimTaskRun(task.Id, task.LastStartUtc, DateTime.UtcNow, LeasedUntilUtc, "instance-a");
        task.TimeInterval = 30;
        await _service.UpdateTask(task);

        var updated = await _service.GetTaskById(task.Id);
        Assert.AreEqual(30, updated.TimeInterval);
        Assert.AreEqual("instance-a", updated.LeasedByInstance);
        Assert.IsNotNull(updated.LeasedUntilUtc);
        Assert.IsTrue(await _service.RenewTaskLease(task.Id, "instance-a", DateTime.UtcNow.AddMinutes(5)));
    }

    [TestMethod]
    public async Task TryClaimTaskRun_ObsoleteOverloadWithoutLease_DoesNotBlockTheNextClaim()
    {
        var task = await InsertTask(DateTime.UtcNow.AddMinutes(-10));
        IScheduleTaskService service = _service;

#pragma warning disable CS0618
        var claimed = await service.TryClaimTaskRun(task.Id, task.LastStartUtc, DateTime.UtcNow, "instance-a");
#pragma warning restore CS0618

        Assert.IsTrue(claimed);
        var updated = await _service.GetTaskById(task.Id);
        Assert.IsFalse(updated.LeasedUntilUtc > DateTime.UtcNow);
    }
}

/// <summary>
/// The lease filter compares nullable dates - checked on LiteDB too, which translates
/// the same expression with its own LINQ provider
/// </summary>
[TestClass]
public class ScheduleTaskServiceLiteDbTests
{
    private ScheduleTaskService _service;

    [TestInitialize]
    public void Init()
    {
        _service = new ScheduleTaskService(new LiteDBRepositoryMock<ScheduleTask>());
    }

    private async Task<ScheduleTask> InsertTask(DateTime? leasedUntilUtc)
    {
        var task = new ScheduleTask {
            ScheduleTaskName = "TestTask",
            Enabled = true,
            TimeInterval = 1,
            LastStartUtc = DateTime.UtcNow.AddMinutes(-10),
            LeasedUntilUtc = leasedUntilUtc,
            LeasedByInstance = "instance-a"
        };
        await _service.InsertTask(task);
        return await _service.GetTaskById(task.Id);
    }

    [TestMethod]
    public async Task TryClaimTaskRun_PreviousRunStillLeased_ReturnsFalse()
    {
        var task = await InsertTask(DateTime.UtcNow.AddMinutes(3));

        var claimed = await _service.TryClaimTaskRun(task.Id, task.LastStartUtc, DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(5), "instance-b");

        Assert.IsFalse(claimed);
    }

    [TestMethod]
    public async Task TryClaimTaskRun_NoLeaseOrLeaseRanOut_ReturnsTrue()
    {
        var released = await InsertTask(null);
        var expired = await InsertTask(DateTime.UtcNow.AddMinutes(-4));

        Assert.IsTrue(await _service.TryClaimTaskRun(released.Id, released.LastStartUtc, DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(5), "instance-b"));
        Assert.IsTrue(await _service.TryClaimTaskRun(expired.Id, expired.LastStartUtc, DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(5), "instance-b"));
    }

    [TestMethod]
    public async Task CompleteTaskRun_OwnLease_ReleasesItAndLeavesTakenOverOneAlone()
    {
        var own = await InsertTask(DateTime.UtcNow.AddMinutes(5));
        var takenOver = await InsertTask(DateTime.UtcNow.AddMinutes(5));

        await _service.CompleteTaskRun(own.Id, "instance-a", true, DateTime.UtcNow, false);
        await _service.CompleteTaskRun(takenOver.Id, "instance-b", true, DateTime.UtcNow, false);

        Assert.IsNull((await _service.GetTaskById(own.Id)).LeasedUntilUtc);
        Assert.IsNotNull((await _service.GetTaskById(takenOver.Id)).LeasedUntilUtc);
    }
}
