using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Grand.Infrastructure.Caching.Message;
using Grand.Infrastructure.Configuration;
using System.Diagnostics;

namespace Grand.Infrastructure.Caching.Redis
{
    public class RedisMessageBus : IMessageBus
    {
        private readonly ISubscriber _subscriber;
        private readonly IServiceProvider _serviceProvider;
        private readonly AppConfig _appConfig;

        private static readonly string _clientId = Guid.NewGuid().ToString("N");

        public RedisMessageBus(ISubscriber subscriber, IServiceProvider serviceProvider, AppConfig appConfig)
        {
            _subscriber = subscriber;
            _serviceProvider = serviceProvider;
            _appConfig = appConfig;
            SubscribeAsync();
        }

        public async Task PublishAsync<TMessage>(TMessage msg) where TMessage : IMessageEvent
        {
            try
            {
                var pub = _subscriber.Multiplexer.GetSubscriber();
                var clientmsg = new MessageEventClient {
                    ClientId = _clientId,
                    Key = msg.Key,
                    MessageType = msg.MessageType
                };
                var message = JsonConvert.SerializeObject(clientmsg);
                await pub.PublishAsync(_appConfig.RedisPubSubChannel, message);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public virtual Task SubscribeAsync()
        {
            var sub = _subscriber.Multiplexer.GetSubscriber();

            sub.SubscribeAsync(_appConfig.RedisPubSubChannel, async (_, redisValue) =>
            {
                try
                {
                    var message = JsonConvert.DeserializeObject<MessageEventClient>(redisValue);
                    if (message != null && message.ClientId != _clientId)
                        await OnSubscriptionChanged(message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });
            return Task.CompletedTask;
        }

        public async Task OnSubscriptionChanged(IMessageEvent message)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var cache = scope.ServiceProvider.GetRequiredService<ICacheBase>();
                switch (message.MessageType)
                {
                    case (int)MessageEventType.RemoveKey:
                        await cache.RemoveAsync(message.Key, false);
                        break;
                    case (int)MessageEventType.RemoveByPrefix:
                        await cache.RemoveByPrefix(message.Key, false);
                        break;
                    case (int)MessageEventType.ClearCache:
                        await cache.Clear(false);
                        break;
                }

            }
        }
    }
}
