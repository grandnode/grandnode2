using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Domain.Tasks;
using Grand.Infrastructure;
using Grand.Web.Common.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

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
