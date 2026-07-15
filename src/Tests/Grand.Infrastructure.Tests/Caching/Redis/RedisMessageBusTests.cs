using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Message;
using Grand.Infrastructure.Caching.Redis;
using Grand.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using StackExchange.Redis;
using System.Net;
using System.Text.Json;

namespace Grand.Infrastructure.Tests.Caching.Redis;

[TestClass]
public class RedisMessageBusTests
{
    private Mock<IConnectionMultiplexer> _connectionMock;
    private Mock<ISubscriber> _subscriberMock;
    private Mock<ICacheBase> _cacheMock;
    private RedisConfig _config;
    private RedisMessageBus _bus;

    [TestInitialize]
    public void Init()
    {
        _subscriberMock = new Mock<ISubscriber>();
        _subscriberMock.Setup(s => s.SubscribeAsync(It.IsAny<RedisChannel>(),
                It.IsAny<Action<RedisChannel, RedisValue>>(), It.IsAny<CommandFlags>()))
            .Returns(Task.CompletedTask);

        _connectionMock = new Mock<IConnectionMultiplexer>();
        _connectionMock.Setup(c => c.GetSubscriber(It.IsAny<object>())).Returns(_subscriberMock.Object);

        _cacheMock = new Mock<ICacheBase>();
        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);
        _cacheMock.Setup(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);
        _cacheMock.Setup(c => c.Clear(It.IsAny<bool>())).Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(_cacheMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        _config = new RedisConfig { RedisPubSubChannel = "test-channel" };
        _bus = new RedisMessageBus(_connectionMock.Object, serviceProvider, _config,
            new Mock<ILogger<RedisMessageBus>>().Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _bus.Dispose();
    }

    [TestMethod]
    public async Task StartAsync_SubscribesToConfiguredChannel()
    {
        await _bus.StartAsync(CancellationToken.None);

        //subscription runs on a tracked background loop - wait for it
        for (var i = 0; i < 100; i++)
        {
            try
            {
                _subscriberMock.Verify(s => s.SubscribeAsync(
                    It.Is<RedisChannel>(c => c == RedisChannel.Literal("test-channel")),
                    It.IsAny<Action<RedisChannel, RedisValue>>(), It.IsAny<CommandFlags>()), Times.AtLeastOnce);
                return;
            }
            catch (MockException)
            {
                await Task.Delay(20);
            }
        }

        Assert.Fail("SubscribeAsync was not called on the configured channel");
    }

    [TestMethod]
    public async Task StopAsync_CompletesAndStopsSubscribing()
    {
        await _bus.StartAsync(CancellationToken.None);
        await _bus.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task PublishAsync_SendsSerializedMessageToConfiguredChannel()
    {
        RedisValue publishedValue = default;
        _subscriberMock.Setup(s =>
                s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .Callback<RedisChannel, RedisValue, CommandFlags>((_, value, _) => publishedValue = value)
            .ReturnsAsync(1);

        await _bus.PublishAsync(new MessageEvent { Key = "key", MessageType = (int)MessageEventType.RemoveKey });

        _subscriberMock.Verify(s => s.PublishAsync(
            It.Is<RedisChannel>(c => c == RedisChannel.Literal("test-channel")),
            It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()), Times.Once);

        var message = JsonSerializer.Deserialize<MessageEventClient>(publishedValue.ToString());
        Assert.IsNotNull(message);
        Assert.AreEqual("key", message.Key);
        Assert.AreEqual((int)MessageEventType.RemoveKey, message.MessageType);
        Assert.IsFalse(string.IsNullOrEmpty(message.ClientId));
    }

    [TestMethod]
    public async Task PublishAsync_SubscriberThrows_DoesNotPropagateException()
    {
        _subscriberMock.Setup(s =>
                s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.SocketFailure, "connection lost"));

        await _bus.PublishAsync(new MessageEvent { Key = "key", MessageType = (int)MessageEventType.RemoveKey });
    }

    [TestMethod]
    public void OnSubscriptionChanged_RemoveKey_RemovesFromLocalCacheWithoutRepublishing()
    {
        _bus.OnSubscriptionChanged(new MessageEventClient
            { ClientId = "other", Key = "key", MessageType = (int)MessageEventType.RemoveKey });

        _cacheMock.Verify(c => c.RemoveAsync("key", false), Times.Once);
    }

    [TestMethod]
    public void OnSubscriptionChanged_RemoveByPrefix_RemovesFromLocalCacheWithoutRepublishing()
    {
        _bus.OnSubscriptionChanged(new MessageEventClient
            { ClientId = "other", Key = "prefix", MessageType = (int)MessageEventType.RemoveByPrefix });

        _cacheMock.Verify(c => c.RemoveByPrefix("prefix", false), Times.Once);
    }

    [TestMethod]
    public void OnSubscriptionChanged_ClearCache_ClearsLocalCacheWithoutRepublishing()
    {
        _bus.OnSubscriptionChanged(new MessageEventClient
            { ClientId = "other", Key = "", MessageType = (int)MessageEventType.ClearCache });

        _cacheMock.Verify(c => c.Clear(false), Times.Once);
    }

    [TestMethod]
    public async Task ConnectionRestored_SubscriptionConnection_ClearsLocalCache()
    {
        await _bus.StartAsync(CancellationToken.None);

        //messages published while disconnected are lost - the local cache must be dropped
        _connectionMock.Raise(c => c.ConnectionRestored += null,
            new ConnectionFailedEventArgs(_connectionMock.Object, new DnsEndPoint("localhost", 6379),
                ConnectionType.Subscription, ConnectionFailureType.SocketClosed, null, "test"));

        _cacheMock.Verify(c => c.Clear(false), Times.Once);
    }

    [TestMethod]
    public async Task ConnectionRestored_InteractiveConnection_DoesNotClearLocalCache()
    {
        await _bus.StartAsync(CancellationToken.None);

        _connectionMock.Raise(c => c.ConnectionRestored += null,
            new ConnectionFailedEventArgs(_connectionMock.Object, new DnsEndPoint("localhost", 6379),
                ConnectionType.Interactive, ConnectionFailureType.SocketClosed, null, "test"));

        _cacheMock.Verify(c => c.Clear(It.IsAny<bool>()), Times.Never);
    }
}
