namespace Grand.Infrastructure.Configuration
{
    public partial class RedisConfig
    {
        /// <summary>
        /// Enable the Publish/Subscribe messaging with redis to manage memory cache on every server
        /// </summary>
        public bool RedisPubSubEnabled { get; set; }

        /// <summary>
        /// Redis connection string. Used when Redis Publish/Subscribe is enabled
        /// </summary>
        public string RedisPubSubConnectionString { get; set; }

        /// <summary>
        /// Messages sent by other clients to these channels will be pushed by Redis to all the subscribed clients. It must me the same value on every server
        /// </summary>
        public string RedisPubSubChannel { get; set; }

        /// <summary>
        /// Indicates whether we should use Redis server for persist keys - required in farm scenario
        /// </summary>
        public bool PersistKeysToRedis { get; set; }

        /// <summary>
        /// Redis connection string. Used when PersistKeysToRedis is enabled
        /// </summary>
        public string PersistKeysToRedisUrl { get; set; }

    }
}
