using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Mediator.Tests;

[TestClass]
public class MediatorPublishTests
{
    private ExecutionLog _log;
    private ServiceProvider _provider;

    [TestInitialize]
    public void Init()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ExecutionLog>();
        services.AddGrandMediator(Assembly.GetExecutingAssembly());
        _provider = services.BuildServiceProvider();
        _log = _provider.GetRequiredService<ExecutionLog>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _provider.Dispose();
    }

    private IMediator Mediator => _provider.GetRequiredService<IMediator>();

    [TestMethod]
    public async Task Publish_InvokesEveryHandler()
    {
        await Mediator.Publish(new SampleEvent());

        CollectionAssert.AreEquivalent(
            new[] { nameof(FirstSampleEventHandler), nameof(SecondSampleEventHandler) }, _log.Entries);
    }

    [TestMethod]
    public async Task Publish_WithoutHandlers_DoesNothing()
    {
        await Mediator.Publish(new UnobservedEvent());

        Assert.AreEqual(0, _log.Entries.Count);
    }

    /// <summary>
    ///     Handlers run in registration order, sequentially - this is the contract documented for domain events
    /// </summary>
    [TestMethod]
    public async Task Publish_RunsHandlersSequentiallyInRegistrationOrder()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ExecutionLog>();
        services.AddTransient<INotificationHandler<SampleEvent>, SecondSampleEventHandler>();
        services.AddTransient<INotificationHandler<SampleEvent>, FirstSampleEventHandler>();
        services.AddTransient<IMediator, Mediator>();

        await using var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<IMediator>().Publish(new SampleEvent());

        CollectionAssert.AreEqual(
            new[] { nameof(SecondSampleEventHandler), nameof(FirstSampleEventHandler) },
            provider.GetRequiredService<ExecutionLog>().Entries);
    }

    /// <summary>
    ///     A throwing handler aborts the remaining ones and surfaces in the caller
    /// </summary>
    [TestMethod]
    public async Task Publish_ThrowingHandler_AbortsRemainingAndPropagates()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ExecutionLog>();
        services.AddTransient<INotificationHandler<FailingEvent>, ThrowingEventHandler>();
        services.AddTransient<INotificationHandler<FailingEvent>, NeverReachedEventHandler>();
        services.AddTransient<IMediator, Mediator>();

        await using var provider = services.BuildServiceProvider();

        await Assert.ThrowsExactlyAsync<InvalidTimeZoneException>(
            async () => await provider.GetRequiredService<IMediator>().Publish(new FailingEvent()));

        CollectionAssert.AreEqual(new[] { nameof(ThrowingEventHandler) },
            provider.GetRequiredService<ExecutionLog>().Entries);
    }

    /// <summary>
    ///     Generic events such as EntityInserted&lt;T&gt; must dispatch on the runtime type
    /// </summary>
    [TestMethod]
    public async Task Publish_DispatchesOnRuntimeTypeOfGenericNotification()
    {
        await Mediator.Publish(new EntityChanged<string>("product"));
        await Mediator.Publish(new EntityChanged<int>(42));

        CollectionAssert.AreEqual(
            new[] { $"{nameof(EntityChangedStringHandler)}:product", $"{nameof(EntityChangedIntHandler)}:42" },
            _log.Entries);
    }

    [TestMethod]
    public async Task Publish_WeaklyTyped_InvokesHandlers()
    {
        await Mediator.Publish((object)new EntityChanged<string>("weak"));

        CollectionAssert.AreEqual(new[] { $"{nameof(EntityChangedStringHandler)}:weak" }, _log.Entries);
    }

    [TestMethod]
    public async Task Publish_WeaklyTyped_NonNotification_Throws()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(async () => await Mediator.Publish(new object()));
    }

    [TestMethod]
    public async Task Publish_Null_Throws()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            async () => await Mediator.Publish((SampleEvent)null));
    }
}
