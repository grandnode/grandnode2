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

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<ScheduleTask>();
        _service = new ScheduleTaskService(_repository);
    }

    private async Task<ScheduleTask> InsertTask(DateTime? lastStartUtc = null)
    {
        var task = new ScheduleTask {
            ScheduleTaskName = "TestTask",
            Enabled = true,
            TimeInterval = 1,
            LastStartUtc = lastStartUtc
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

        var claimed = await _service.TryClaimTaskRun(task.Id, task.LastStartUtc, runStartUtc, "instance-a");

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
        var claimedByA = await _service.TryClaimTaskRun(task.Id, task.LastStartUtc, DateTime.UtcNow, "instance-a");
        //instance B still holds the old LastStartUtc value - its claim must fail
        var claimedByB = await _service.TryClaimTaskRun(task.Id, task.LastStartUtc, DateTime.UtcNow, "instance-b");

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
            _service.TryClaimTaskRun(task.Id, task.LastStartUtc, DateTime.UtcNow, "instance-1"),
            _service.TryClaimTaskRun(task.Id, task.LastStartUtc, DateTime.UtcNow, "instance-2"));

        Assert.AreEqual(1, claims.Count(x => x));
    }

    [TestMethod]
    public async Task TryClaimTaskRun_NonExistingTask_ReturnsFalse()
    {
        var claimed = await _service.TryClaimTaskRun("000000000000000000000000", null, DateTime.UtcNow, "instance-a");

        Assert.IsFalse(claimed);
    }
}
