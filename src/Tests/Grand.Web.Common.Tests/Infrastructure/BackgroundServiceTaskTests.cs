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
                It.IsAny<DateTime>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var service = new BackgroundServiceTask(TaskName, _serviceProvider);
        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);

        await WaitFor(() => _scheduleTaskServiceMock.Invocations.Any(i =>
            i.Method.Name == nameof(IScheduleTaskService.UpdateTask)));
        cts.Cancel();

        _scheduleTaskMock.Verify(t => t.Execute(), Times.Once);
        _scheduleTaskServiceMock.Verify(s => s.UpdateTask(It.Is<ScheduleTask>(x =>
            x.LastSuccessUtc != null && x.LastStartUtc != null)), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_ClaimLost_DoesNotExecuteAndDoesNotPersist()
    {
        _scheduleTaskServiceMock.Setup(s => s.TryClaimTaskRun(_task.Id, It.IsAny<DateTime?>(),
                It.IsAny<DateTime>(), It.IsAny<string>()))
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
            It.IsAny<DateTime>(), It.IsAny<string>()), Times.Never);
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
        var decision = Decide(null, MachineName, UtcNow);

        Assert.AreEqual(ScheduleAction.Retry, decision.Action);
        Assert.AreEqual(1, decision.DelayMinutes);
    }

    [TestMethod]
    public void Decide_TaskDisabled_ReturnsRetryAfterOwnInterval()
    {
        var task = NewTask(enabled: false, timeInterval: 15);

        var decision = Decide(task, MachineName, UtcNow);

        Assert.AreEqual(ScheduleAction.Retry, decision.Action);
        Assert.AreEqual(15, decision.DelayMinutes);
    }

    [TestMethod]
    public void Decide_TaskLeasedByAnotherMachine_ReturnsRetryAfterOwnInterval()
    {
        var task = NewTask(leasedBy: "other-machine", timeInterval: 15);

        var decision = Decide(task, MachineName, UtcNow);

        Assert.AreEqual(ScheduleAction.Retry, decision.Action);
        Assert.AreEqual(15, decision.DelayMinutes);
    }

    [TestMethod]
    public void Decide_TaskLeasedByOwnMachine_IsEligible()
    {
        var task = NewTask(leasedBy: MachineName);

        var decision = Decide(task, MachineName, UtcNow);

        Assert.AreEqual(ScheduleAction.RunNow, decision.Action);
    }

    [TestMethod]
    public void Decide_EnabledAndNeverRunBefore_ReturnsRunNow()
    {
        var task = NewTask(timeInterval: 30);

        var decision = Decide(task, MachineName, UtcNow);

        Assert.AreEqual(ScheduleAction.RunNow, decision.Action);
        Assert.AreEqual(30, decision.DelayMinutes);
    }

    [TestMethod]
    public void Decide_EnabledAndIntervalElapsed_ReturnsRunNow()
    {
        var task = NewTask(timeInterval: 30, lastStartUtc: UtcNow.AddMinutes(-31));

        var decision = Decide(task, MachineName, UtcNow);

        Assert.AreEqual(ScheduleAction.RunNow, decision.Action);
        Assert.AreEqual(30, decision.DelayMinutes);
    }

    [TestMethod]
    public void Decide_EnabledButNotDueYet_ReturnsWaitForRemainingTimeTruncatedToWholeMinutes()
    {
        //10.5 minutes elapsed of a 30-minute interval -> 19.5 minutes remain
        var task = NewTask(timeInterval: 30, lastStartUtc: UtcNow.AddSeconds(-(10 * 60 + 30)));

        var decision = Decide(task, MachineName, UtcNow);

        Assert.AreEqual(ScheduleAction.WaitForNextRun, decision.Action);
        Assert.AreEqual(19, decision.DelayMinutes); //truncated, same as the original code
    }
}
