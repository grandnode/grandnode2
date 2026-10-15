using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Domain.Tasks;
using Grand.Infrastructure;
using Grand.Web.Common.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static Grand.Web.Common.Infrastructure.BackgroundServiceTask;

namespace Grand.Web.Common.Tests.Infrastructure;

[TestClass]
public class BackgroundServiceTaskTests
{
    private const string TaskName = "TestTask";

    private Mock<IScheduleTaskService> _scheduleTaskServiceMock;
    private Mock<IScheduleTask> _scheduleTaskMock;
    private ScheduleTask _task;
    private IServiceProvider _serviceProvider;

    [TestInitialize]
    public void Init()
    {
        _task = new ScheduleTask {
            Id = "1",
            ScheduleTaskName = TaskName,
            Enabled = true,
            TimeInterval = 60
        };

        _scheduleTaskServiceMock = new Mock<IScheduleTaskService>();
        _scheduleTaskServiceMock.Setup(s => s.GetTaskByName(TaskName)).ReturnsAsync(_task);
        _scheduleTaskServiceMock.Setup(s => s.UpdateTask(It.IsAny<ScheduleTask>()))
            .Returns(Task.CompletedTask);

        _scheduleTaskMock = new Mock<IScheduleTask>();
        _scheduleTaskMock.Setup(t => t.Execute()).Returns(Task.CompletedTask);

        var storeContextSetter = new Mock<IStoreContextSetter>();
        storeContextSetter.Setup(s => s.InitializeStoreContext(It.IsAny<string>()))
            .ReturnsAsync(Mock.Of<IStoreContext>());
        var workContextSetter = new Mock<IWorkContextSetter>();
        workContextSetter.Setup(s => s.InitializeWorkContext(It.IsAny<string>()))
            .ReturnsAsync(Mock.Of<IWorkContext>());

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_scheduleTaskServiceMock.Object);
        services.AddKeyedSingleton(TaskName, _scheduleTaskMock.Object);
        services.AddSingleton(Mock.Of<IContextAccessor>());
        services.AddSingleton(storeContextSetter.Object);
        services.AddSingleton(workContextSetter.Object);
        _serviceProvider = services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task ExecuteAsync_ClaimWon_ExecutesTaskAndPersistsResult()
    {
        _scheduleTaskServiceMock.Setup(s => s.TryClaimTaskRun(_task.Id, It.IsAny<DateTime?>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var service = new BackgroundServiceTask(TaskName, _serviceProvider);
        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);

        await WaitFor(() => _scheduleTaskServiceMock.Invocations.Any(i =>
            i.Method.Name == nameof(IScheduleTaskService.CompleteTaskRun)));
        cts.Cancel();

        _scheduleTaskMock.Verify(t => t.Execute(), Times.Once);
        _scheduleTaskServiceMock.Verify(s => s.CompleteTaskRun(_task.Id, It.IsAny<string>(), true,
            It.IsAny<DateTime>(), false), Times.Once);
        //a write of the whole entity read before the run would revert admin edits made during it
        _scheduleTaskServiceMock.Verify(s => s.UpdateTask(It.IsAny<ScheduleTask>()), Times.Never);
    }

    [TestMethod]
    public async Task ExecuteAsync_ClaimWon_LeasesTheRunAndReleasesItWhenDone()
    {
        DateTime? claimedRunStartUtc = null, claimedLeasedUntilUtc = null;
        string claimingInstanceId = null;
        _scheduleTaskServiceMock.Setup(s => s.TryClaimTaskRun(_task.Id, It.IsAny<DateTime?>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()))
            .Callback<string, DateTime?, DateTime, DateTime, string>((_, _, runStartUtc, leasedUntilUtc, instanceId) => {
                claimedRunStartUtc = runStartUtc;
                claimedLeasedUntilUtc = leasedUntilUtc;
                claimingInstanceId = instanceId;
            })
            .ReturnsAsync(true);

        var service = new BackgroundServiceTask(TaskName, _serviceProvider);
        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);

        await WaitFor(() => _scheduleTaskServiceMock.Invocations.Any(i =>
            i.Method.Name == nameof(IScheduleTaskService.CompleteTaskRun)));
        cts.Cancel();

        Assert.AreEqual(LeaseDuration, claimedLeasedUntilUtc - claimedRunStartUtc);
        //released as the instance that claimed the run - a takeover by another instance is left alone
        _scheduleTaskServiceMock.Verify(s => s.CompleteTaskRun(_task.Id, claimingInstanceId, It.IsAny<bool>(),
            It.IsAny<DateTime>(), It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_TaskFailsAndStopsOnError_RecordsFailureAndDisablesTask()
    {
        _task.StopOnError = true;
        _scheduleTaskServiceMock.Setup(s => s.TryClaimTaskRun(_task.Id, It.IsAny<DateTime?>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _scheduleTaskMock.Setup(t => t.Execute()).ThrowsAsync(new InvalidOperationException("task failure"));

        var service = new BackgroundServiceTask(TaskName, _serviceProvider);
        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);

        await WaitFor(() => _scheduleTaskServiceMock.Invocations.Any(i =>
            i.Method.Name == nameof(IScheduleTaskService.CompleteTaskRun)));
        cts.Cancel();

        _scheduleTaskServiceMock.Verify(s => s.CompleteTaskRun(_task.Id, It.IsAny<string>(), false,
            It.IsAny<DateTime>(), true), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_ClaimWon_TaskSeesBackgroundWorkAndStoreContext()
    {
        _task.StoreId = "store-1";
        _scheduleTaskServiceMock.Setup(s => s.TryClaimTaskRun(_task.Id, It.IsAny<DateTime?>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var storeContext = Mock.Of<IStoreContext>();
        var workContext = Mock.Of<IWorkContext>();
        var storeContextSetter = new Mock<IStoreContextSetter>();
        storeContextSetter.Setup(s => s.InitializeStoreContext("store-1")).ReturnsAsync(storeContext);
        var workContextSetter = new Mock<IWorkContextSetter>();
        workContextSetter.Setup(s => s.InitializeWorkContext("store-1")).ReturnsAsync(workContext);

        //the real AsyncLocal-backed accessor - a mock cannot show whether the values flow into Execute
        var contextAccessor = new ContextAccessor();
        IWorkContext seenWorkContext = null;
        IStoreContext seenStoreContext = null;
        _scheduleTaskMock.Setup(t => t.Execute()).Returns(async () => {
            await Task.Yield();
            seenWorkContext = contextAccessor.WorkContext;
            seenStoreContext = contextAccessor.StoreContext;
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_scheduleTaskServiceMock.Object);
        services.AddKeyedSingleton(TaskName, _scheduleTaskMock.Object);
        services.AddSingleton<IContextAccessor>(contextAccessor);
        services.AddSingleton(storeContextSetter.Object);
        services.AddSingleton(workContextSetter.Object);

        var service = new BackgroundServiceTask(TaskName, services.BuildServiceProvider());
        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);

        await WaitFor(() => _scheduleTaskServiceMock.Invocations.Any(i =>
            i.Method.Name == nameof(IScheduleTaskService.CompleteTaskRun)));
        cts.Cancel();

        Assert.AreSame(workContext, seenWorkContext);
        Assert.AreSame(storeContext, seenStoreContext);
    }

    [TestMethod]
    public async Task ExecuteAsync_ClaimLost_DoesNotExecuteAndDoesNotPersist()
    {
        _scheduleTaskServiceMock.Setup(s => s.TryClaimTaskRun(_task.Id, It.IsAny<DateTime?>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var service = new BackgroundServiceTask(TaskName, _serviceProvider);
        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);

        await WaitFor(() => _scheduleTaskServiceMock.Invocations.Any(i =>
            i.Method.Name == nameof(IScheduleTaskService.TryClaimTaskRun)));
        //give the loop a moment to (incorrectly) execute the task if the guard is broken
        await Task.Delay(200);
        cts.Cancel();

        _scheduleTaskMock.Verify(t => t.Execute(), Times.Never);
        //losing instance must not overwrite the winner's data with a stale entity
        _scheduleTaskServiceMock.Verify(s => s.UpdateTask(It.IsAny<ScheduleTask>()), Times.Never);
        _scheduleTaskServiceMock.Verify(s => s.CompleteTaskRun(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<bool>(), It.IsAny<DateTime>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task ExecuteAsync_TaskNotDueYet_DoesNotClaimNorExecute()
    {
        _task.LastStartUtc = DateTime.UtcNow;

        var service = new BackgroundServiceTask(TaskName, _serviceProvider);
        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);

        await WaitFor(() => _scheduleTaskServiceMock.Invocations.Any(i =>
            i.Method.Name == nameof(IScheduleTaskService.GetTaskByName)));
        await Task.Delay(200);
        cts.Cancel();

        _scheduleTaskServiceMock.Verify(s => s.TryClaimTaskRun(It.IsAny<string>(), It.IsAny<DateTime?>(),
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()), Times.Never);
        _scheduleTaskMock.Verify(t => t.Execute(), Times.Never);
        _scheduleTaskServiceMock.Verify(s => s.UpdateTask(It.IsAny<ScheduleTask>()), Times.Never);
    }

    [TestMethod]
    public async Task ExecuteAsync_TaskLeasedByOtherMachine_DoesNotExecute()
    {
        _task.LeasedByMachineName = "other-machine";

        var service = new BackgroundServiceTask(TaskName, _serviceProvider);
        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);

        await WaitFor(() => _scheduleTaskServiceMock.Invocations.Any(i =>
            i.Method.Name == nameof(IScheduleTaskService.GetTaskByName)));
        await Task.Delay(200);
        cts.Cancel();

        _scheduleTaskMock.Verify(t => t.Execute(), Times.Never);
    }

    [TestMethod]
    public async Task ExecuteAsync_UnexpectedExceptionInLoop_LogsError()
    {
        _scheduleTaskServiceMock.Setup(s => s.GetTaskByName(TaskName))
            .ThrowsAsync(new InvalidOperationException("transient failure"));

        var loggerMock = new Mock<ILogger<BackgroundServiceTask>>();
        var services = new ServiceCollection();
        services.AddSingleton(_scheduleTaskServiceMock.Object);
        services.AddKeyedSingleton(TaskName, _scheduleTaskMock.Object);
        services.AddSingleton(Mock.Of<IContextAccessor>());
        services.AddSingleton(Mock.Of<IStoreContextSetter>());
        services.AddSingleton(Mock.Of<IWorkContextSetter>());
        services.AddSingleton(loggerMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var service = new BackgroundServiceTask(TaskName, serviceProvider);
        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);

        await WaitFor(() => loggerMock.Invocations.Any(i => i.Method.Name == "Log"));
        cts.Cancel();

        //the failure must be logged, not swallowed silently
        loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.Is<Exception>(e => e is InvalidOperationException),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    private static async Task WaitFor(Func<bool> condition)
    {
        for (var i = 0; i < 300; i++)
        {
            if (condition()) return;
            await Task.Delay(10);
        }

        Assert.Fail("Condition was not met within the timeout");
    }
}

/// <summary>
/// Pure unit tests for the scheduling decision - no timers, no DI, no BackgroundService
/// lifecycle. This is what actually proves REL-001 is fixed: every branch resolves to a
/// delay-and-retry, none of them can signal "stop the loop".
/// </summary>
[TestClass]
public class BackgroundServiceTaskDecideTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc);
    private const string MachineName = "this-machine";

    private static ScheduleTask NewTask(bool enabled = true, string leasedBy = null,
        int timeInterval = 60, DateTime? lastStartUtc = null)
    {
        return new ScheduleTask {
            Id = "1",
            Enabled = enabled,
            LeasedByMachineName = leasedBy,
            TimeInterval = timeInterval,
            LastStartUtc = lastStartUtc
        };
    }

    [TestMethod]
    public void Decide_TaskNotSeededYet_ReturnsRetryInOneMinute()
    {
        var decision = Decide(null, MachineName, null, UtcNow);

        Assert.AreEqual(ScheduleAction.Retry, decision.Action);
        Assert.AreEqual(TimeSpan.FromMinutes(1), decision.Delay);
    }

    [TestMethod]
    public void Decide_TaskDisabled_ReturnsRetryAfterOwnInterval()
    {
        var task = NewTask(enabled: false, timeInterval: 15);

        var decision = Decide(task, MachineName, null, UtcNow);

        Assert.AreEqual(ScheduleAction.Retry, decision.Action);
        Assert.AreEqual(TimeSpan.FromMinutes(15), decision.Delay);
    }

    [TestMethod]
    public void Decide_TaskLeasedByAnotherMachine_ReturnsRetryAfterOwnInterval()
    {
        var task = NewTask(leasedBy: "other-machine", timeInterval: 15);

        var decision = Decide(task, MachineName, null, UtcNow);

        Assert.AreEqual(ScheduleAction.Retry, decision.Action);
        Assert.AreEqual(TimeSpan.FromMinutes(15), decision.Delay);
    }

    [TestMethod]
    public void Decide_TaskLeasedByOwnMachine_IsEligible()
    {
        var task = NewTask(leasedBy: MachineName);

        var decision = Decide(task, MachineName, null, UtcNow);

        Assert.AreEqual(ScheduleAction.RunNow, decision.Action);
    }

    [TestMethod]
    public void Decide_EnabledAndNeverRunBefore_ReturnsRunNow()
    {
        var task = NewTask(timeInterval: 30);

        var decision = Decide(task, MachineName, null, UtcNow);

        Assert.AreEqual(ScheduleAction.RunNow, decision.Action);
        Assert.AreEqual(TimeSpan.FromMinutes(30), decision.Delay);
    }

    [TestMethod]
    public void Decide_EnabledAndIntervalElapsed_ReturnsRunNow()
    {
        var task = NewTask(timeInterval: 30, lastStartUtc: UtcNow.AddMinutes(-31));

        var decision = Decide(task, MachineName, null, UtcNow);

        Assert.AreEqual(ScheduleAction.RunNow, decision.Action);
        Assert.AreEqual(TimeSpan.FromMinutes(30), decision.Delay);
    }

    [TestMethod]
    public void Decide_EnabledButNotDueYet_ReturnsWaitForExactRemainingTime()
    {
        //10.5 minutes elapsed of a 30-minute interval -> 19.5 minutes remain
        var task = NewTask(timeInterval: 30, lastStartUtc: UtcNow.AddSeconds(-(10 * 60 + 30)));

        var decision = Decide(task, MachineName, null, UtcNow);

        Assert.AreEqual(ScheduleAction.WaitForNextRun, decision.Action);
        Assert.AreEqual(TimeSpan.FromSeconds(19 * 60 + 30), decision.Delay);
    }

    [TestMethod]
    public void Decide_LessThanAMinuteBeforeNextRun_WaitsTheRemainingSecondsNotZero()
    {
        //truncating to whole minutes gave 0 here - the loop then re-read the task without any delay
        var task = NewTask(timeInterval: 30, lastStartUtc: UtcNow.AddSeconds(-(29 * 60 + 20)));

        var decision = Decide(task, MachineName, null, UtcNow);

        Assert.AreEqual(ScheduleAction.WaitForNextRun, decision.Action);
        Assert.AreEqual(TimeSpan.FromSeconds(40), decision.Delay);
    }

    [TestMethod]
    public void Decide_PinnedToConfiguredInstanceName_IsEligible()
    {
        //container hostnames change on every deployment - the configured name does not
        var task = NewTask(leasedBy: "worker");

        var decision = Decide(task, "grandnode-7f9c4b-x2kq", "worker", UtcNow);

        Assert.AreEqual(ScheduleAction.RunNow, decision.Action);
    }

    [TestMethod]
    public void Decide_PinnedToMachineName_IgnoresCase()
    {
        var task = NewTask(leasedBy: "THIS-MACHINE");

        var decision = Decide(task, MachineName, null, UtcNow);

        Assert.AreEqual(ScheduleAction.RunNow, decision.Action);
    }

    [TestMethod]
    public void Decide_PinnedToAnotherInstanceName_ReturnsRetry()
    {
        var task = NewTask(leasedBy: "worker", timeInterval: 15);

        var decision = Decide(task, MachineName, "web", UtcNow);

        Assert.AreEqual(ScheduleAction.Retry, decision.Action);
        Assert.AreEqual(TimeSpan.FromMinutes(15), decision.Delay);
    }

    [TestMethod]
    public void Decide_DueButPreviousRunStillLeased_RetriesWhenLeaseRunsOut()
    {
        var task = NewTask(timeInterval: 1, lastStartUtc: UtcNow.AddMinutes(-3));
        task.LeasedUntilUtc = UtcNow.AddMinutes(2);

        var decision = Decide(task, MachineName, null, UtcNow);

        Assert.AreEqual(ScheduleAction.Retry, decision.Action);
        Assert.AreEqual(TimeSpan.FromMinutes(2), decision.Delay);
    }

    [TestMethod]
    public void Decide_DueAndPreviousLeaseRanOut_ReturnsRunNow()
    {
        var task = NewTask(timeInterval: 1, lastStartUtc: UtcNow.AddMinutes(-10));
        task.LeasedUntilUtc = UtcNow.AddMinutes(-4);

        var decision = Decide(task, MachineName, null, UtcNow);

        Assert.AreEqual(ScheduleAction.RunNow, decision.Action);
    }

    [TestMethod]
    public void Decide_DatesReadAsLocalTime_AreComparedInUtc()
    {
        //LiteDB returns dates as local time - the same instants as the UTC ones
        var task = NewTask(timeInterval: 30, lastStartUtc: UtcNow.AddMinutes(-10).ToLocalTime());

        var decision = Decide(task, MachineName, null, UtcNow);

        Assert.AreEqual(ScheduleAction.WaitForNextRun, decision.Action);
        Assert.AreEqual(TimeSpan.FromMinutes(20), decision.Delay);
    }
}
