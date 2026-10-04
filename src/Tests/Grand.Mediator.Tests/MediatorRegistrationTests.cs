using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Mediator.Tests;

[TestClass]
public class MediatorRegistrationTests
{
    private static readonly Assembly Current = Assembly.GetExecutingAssembly();

    [TestMethod]
    public void AddGrandMediator_RegistersMediator()
    {
        var services = new ServiceCollection();
        services.AddGrandMediator(Current);

        Assert.AreEqual(1, services.Count(x => x.ServiceType == typeof(IMediator)));
        Assert.AreEqual(typeof(Mediator), services.Single(x => x.ServiceType == typeof(IMediator))
            .ImplementationType);
    }

    [TestMethod]
    public void AddGrandMediator_RegistersRequestHandlerAsTransient()
    {
        var services = new ServiceCollection();
        services.AddGrandMediator(Current);

        var descriptor = services.Single(x => x.ServiceType == typeof(IRequestHandler<PingQuery, string>));

        Assert.AreEqual(typeof(PingQueryHandler), descriptor.ImplementationType);
        Assert.AreEqual(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [TestMethod]
    public void AddGrandMediator_RegistersEveryNotificationHandler()
    {
        var services = new ServiceCollection();
        services.AddGrandMediator(Current);

        var implementations = services
            .Where(x => x.ServiceType == typeof(INotificationHandler<SampleEvent>))
            .Select(x => x.ImplementationType)
            .ToList();

        CollectionAssert.AreEquivalent(
            new[] { typeof(FirstSampleEventHandler), typeof(SecondSampleEventHandler) }, implementations);
    }

    /// <summary>
    ///     StartupBase registers assembly by assembly and may see the same assembly more than once
    /// </summary>
    [TestMethod]
    public void AddGrandMediator_IsIdempotent()
    {
        var once = new ServiceCollection();
        once.AddGrandMediator(Current);

        var twice = new ServiceCollection();
        twice.AddGrandMediator(Current);
        twice.AddGrandMediator(Current);

        Assert.AreEqual(once.Count, twice.Count);
    }

    /// <summary>
    ///     Open generic handlers are left to the hand-written registrations that close them
    /// </summary>
    [TestMethod]
    public void AddGrandMediator_SkipsOpenGenericHandlers()
    {
        var services = new ServiceCollection();
        services.AddGrandMediator(Current);

        Assert.IsFalse(services.Any(x => x.ImplementationType == typeof(GenericQueryHandler<>)));
    }

    [TestMethod]
    public void AddGrandMediator_AcceptsAssemblyCollection()
    {
        var services = new ServiceCollection();
        services.AddGrandMediator([Current, Current]);

        Assert.IsTrue(services.Any(x => x.ServiceType == typeof(IRequestHandler<PingQuery, string>)));
    }
}
