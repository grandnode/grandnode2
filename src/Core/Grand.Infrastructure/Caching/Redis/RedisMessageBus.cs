using Newtonsoft.Json;
using StackExchange.Redis;
using Microsoft.Extensions.DependencyInjection;
using Grand.Infrastructure.Caching.Message;
using Grand.Infrastructure.Configuration;
using System.Diagnostics;

namespace Grand.Infrastructure.Caching.Redis
{
    public sealed class RedisMessageBus : IMessageBus
    {
        private readonly ISubscriber _subscriber;
        private readonly IServiceProvider _serviceProvider;
        private readonly RedisConfig _redisConfig;

        private static readonly string ClientId = Guid.NewGuid().ToString("N");

        public RedisMessageBus(ISubscriber subscriber, IServiceProvider serviceProvider, RedisConfig redisConfig)
        {
            _subscriber = subscriber;
            _serviceProvider = serviceProvider;
            _redisConfig = redisConfig;
            SubscribeAsync();
        }

        public async Task PublishAsync<TMessage>(TMessage msg) where TMessage : IMessageEvent
        {
            try
            {
                var client = new MessageEventClient {
                    ClientId = ClientId,
                    Key = msg.Key,
                    MessageType = msg.MessageType
                };
                var message = JsonConvert.SerializeObject(client);
                await _subscriber.PublishAsync(RedisChannel.Literal(_redisConfig.RedisPubSubChannel), message);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public Task SubscribeAsync()
        {
            _subscriber.SubscribeAsync(RedisChannel.Literal(_redisConfig.RedisPubSubChannel),  (_, redisValue) =>
            {
                try
                {
                    var message = JsonConvert.DeserializeObject<MessageEventClient>(redisValue);
                    if (message != null && message.ClientId != ClientId)
                        OnSubscriptionChanged(message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });
            return Task.CompletedTask;
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
    }
}
