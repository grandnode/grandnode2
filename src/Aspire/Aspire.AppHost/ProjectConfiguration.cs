namespace Aspire.AppHost;

public static class ProjectConfiguration
{
    public static void ConfigureGrandWebProject(this IDistributedApplicationBuilder builder,
        IResourceBuilder<MongoDBDatabaseResource> mongodb,
        IResourceBuilder<RedisResource> redis)
    {
        //two instances of the same application (multi-pod simulation) sharing one database
        //and synchronizing their memory cache through the Redis pub/sub message bus
        builder.AddGrandWebInstance("grand-web", 80, mongodb, redis);
        builder.AddGrandWebInstance("grand-web-2", 8080, mongodb, redis);
    }

    private static void AddGrandWebInstance(this IDistributedApplicationBuilder builder, string name, int port,
        IResourceBuilder<MongoDBDatabaseResource> mongodb,
        IResourceBuilder<RedisResource> redis)
    {
        builder
            .AddProject<Projects.Grand_Web>(name)
            .WithHttpEndpoint(port, name: "front")
            .WithReference(mongodb)
            .WaitFor(mongodb)
            .WaitFor(redis)
            //both instances share one content root - shadow copy would make them fight
            //over the same Plugins/bin folder (files locked by the other instance)
            .WithEnvironment("Extensions__PluginShadowCopy", "false")
            .WithEnvironment("Redis__RedisPubSubEnabled", "true")
            //show publish/receive debug logs of the cache message bus in the dashboard
            .WithEnvironment("Logging__LogLevel__Grand.Infrastructure.Caching.Redis", "Debug")
            .WithEnvironment("Redis__RedisPubSubChannel", "grandnode-cache")
            .WithEnvironment("Redis__RedisPubSubConnectionString", redis.Resource.ConnectionStringExpression);
    }
}
