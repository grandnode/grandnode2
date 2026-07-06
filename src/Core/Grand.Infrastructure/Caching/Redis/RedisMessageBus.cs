using Grand.Infrastructure.Caching.Message;
using Grand.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Grand.Infrastructure.Caching.Redis;

public sealed class RedisMessageBus : IMessageBus, IDisposable
{
    private static readonly string ClientId = Guid.NewGuid().ToString("N");

    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);

    private readonly IConnectionMultiplexer _connection;
    private readonly RedisConfig _redisConfig;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISubscriber _subscriber;
    private readonly ILogger<RedisMessageBus> _logger;
    private readonly CancellationTokenSource _cts = new();

    public RedisMessageBus(IConnectionMultiplexer connection, IServiceProvider serviceProvider,
        RedisConfig redisConfig, ILogger<RedisMessageBus> logger)
    {
        _connection = connection;
        _subscriber = connection.GetSubscriber();
        _serviceProvider = serviceProvider;
        _redisConfig = redisConfig;
        _logger = logger;

        _connection.ConnectionFailed += OnConnectionFailed;
        _connection.ConnectionRestored += OnConnectionRestored;

        _ = Task.Run(() => SubscribeWithRetryAsync(_cts.Token));
    }

    public async Task PublishAsync<TMessage>(TMessage msg) where TMessage : IMessageEvent
    {
        var message = JsonSerializer.Serialize(new MessageEventClient {
            ClientId = ClientId,
            Key = msg.Key,
            MessageType = msg.MessageType
        });
        try
        {
            var receivers =
                await _subscriber.PublishAsync(RedisChannel.Literal(_redisConfig.RedisPubSubChannel), message);
            _logger.LogDebug(
                "Published cache invalidation message (type: {MessageType}, key: {Key}), delivered to {Receivers} subscriber(s)",
                (MessageEventType)msg.MessageType, msg.Key, receivers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish cache invalidation message (type: {MessageType}, key: {Key}) - other instances may serve stale data until cache expiration",
                (MessageEventType)msg.MessageType, msg.Key);
        }
    }

    public Task SubscribeAsync()
    {
        return _subscriber.SubscribeAsync(RedisChannel.Literal(_redisConfig.RedisPubSubChannel), OnMessage);
    }

    public void OnSubscriptionChanged(IMessageEvent message)
    {
        using var scope = _serviceProvider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheBase>();
        switch (message.MessageType)
        {
            case (int)MessageEventType.RemoveKey:
                _ = cache.RemoveAsync(message.Key, false);
                break;
            case (int)MessageEventType.RemoveByPrefix:
                _ = cache.RemoveByPrefix(message.Key, false);
                break;
            case (int)MessageEventType.ClearCache:
                _ = cache.Clear(false);
                break;
        }
    }

    public void Dispose()
    {
        _connection.ConnectionFailed -= OnConnectionFailed;
        _connection.ConnectionRestored -= OnConnectionRestored;
        _cts.Cancel();
        _cts.Dispose();
    }

    private async Task SubscribeWithRetryAsync(CancellationToken cancellationToken)
    {
        var delay = InitialRetryDelay;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await SubscribeAsync();
                _logger.LogInformation("Subscribed to Redis pub/sub channel {Channel}",
                    _redisConfig.RedisPubSubChannel);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to subscribe to Redis pub/sub channel {Channel}, retrying in {Delay}s",
                    _redisConfig.RedisPubSubChannel, delay.TotalSeconds);
                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                delay = delay * 2 > MaxRetryDelay ? MaxRetryDelay : delay * 2;
            }
        }
    }

    private void OnMessage(RedisChannel channel, RedisValue redisValue)
    {
        try
        {
            if (redisValue.IsNull) return;
            var message = JsonSerializer.Deserialize<MessageEventClient>(redisValue.ToString());
            if (message != null && message.ClientId != ClientId)
            {
                _logger.LogDebug(
                    "Received cache invalidation message (type: {MessageType}, key: {Key}) from client {ClientId}",
                    (MessageEventType)message.MessageType, message.Key, message.ClientId);
                OnSubscriptionChanged(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process cache invalidation message from Redis");
        }
    }

    private void OnConnectionFailed(object sender, ConnectionFailedEventArgs e)
    {
        _logger.LogWarning(e.Exception,
            "Redis connection failed ({ConnectionType}, {FailureType}) - cache invalidation messages may be lost while disconnected",
            e.ConnectionType, e.FailureType);
    }

    private void OnConnectionRestored(object sender, ConnectionFailedEventArgs e)
    {
        //invalidation messages published while this instance was disconnected are lost
        //(Redis pub/sub has no replay), so the local cache can no longer be trusted
        if (e.ConnectionType != ConnectionType.Subscription) return;
        _logger.LogWarning(
            "Redis subscription connection restored - clearing local cache to drop entries that may have missed invalidation");
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cache = scope.ServiceProvider.GetRequiredService<ICacheBase>();
            _ = cache.Clear(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear local cache after Redis connection was restored");
        }
    }
}
