using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Mediator.Tests;

[TestClass]
public class MediatorSendTests
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
    public async Task Send_ReturnsResponseFromHandler()
    {
        var result = await Mediator.Send(new PingQuery { Message = "hello" });

        Assert.AreEqual("pong:hello", result);
    }

    [TestMethod]
    public async Task Send_WithoutResponse_InvokesHandler()
    {
        await Mediator.Send(new VoidCommand());

        CollectionAssert.AreEqual(new[] { nameof(VoidCommandHandler) }, _log.Entries);
    }

    [TestMethod]
    public async Task Send_WithoutHandler_Throws()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await Mediator.Send(new UnhandledQuery()));
    }

    [TestMethod]
    public async Task Send_WithoutResponseAndWithoutHandler_Throws()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await Mediator.Send(new UnhandledCommand()));
    }

    [TestMethod]
    public async Task Send_Null_Throws()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            async () => await Mediator.Send((IRequest<string>)null));
    }

    [TestMethod]
    public async Task Send_WeaklyTyped_ReturnsResponse()
    {
        var result = await Mediator.Send((object)new PingQuery { Message = "weak" });

        Assert.AreEqual("pong:weak", result);
    }

    [TestMethod]
    public async Task Send_WeaklyTyped_WithoutResponse_ReturnsNull()
    {
        var result = await Mediator.Send((object)new VoidCommand());

        Assert.IsNull(result);
        CollectionAssert.AreEqual(new[] { nameof(VoidCommandHandler) }, _log.Entries);
    }

    [TestMethod]
    public async Task Send_WeaklyTyped_NonRequest_Throws()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(async () => await Mediator.Send(new object()));
    }

    /// <summary>
    ///     Grand.Module.Api registers closed generic handlers by hand and as scoped - the mediator must resolve
    ///     them through the container rather than from an internal registry built by scanning
    /// </summary>
    [TestMethod]
    public async Task Send_ResolvesHandRegisteredScopedClosedGeneric()
    {
        var services = new ServiceCollection();
        services.AddGrandMediator(Assembly.GetExecutingAssembly());
        services.AddScoped(typeof(IRequestHandler<GenericQuery<string>, IList<string>>),
            typeof(GenericQueryHandler<string>));

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<IMediator>()
            .Send(new GenericQuery<string>());

        Assert.AreEqual(1, result.Count);
    }
}
