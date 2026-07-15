namespace Aspire.AppHost;

public static class ProjectConfiguration
{
    public static void ConfigureGrandWebProject(this IDistributedApplicationBuilder builder,
        IResourceBuilder<MongoDBDatabaseResource> mongodb,
        IResourceBuilder<RedisResource> redis)
    {
        //run two replicas of the same application (multi-pod simulation) sharing one database
        //and synchronizing their memory cache through the Redis pub/sub message bus.
        //replicas (not two separate AddProject resources) so Aspire builds the project once and
        //launches N processes - two independent builds would race over the same bin/ output and
        //crash a process with host error 0x80008081. Aspire assigns each replica its own port,
        //so no fixed WithHttpEndpoint here.
        builder
            .AddProject<Projects.Grand_Web>("grand-web")
            .WithReplicas(2)
            .WithReference(mongodb)
            .WaitFor(mongodb)
            .WaitFor(redis)
            //replicas share one content root - shadow copy would make them fight over the same
            //Plugins/bin folder (files locked by the other replica)
            .WithEnvironment("Extensions__PluginShadowCopy", "false")
            .WithEnvironment("Redis__RedisPubSubEnabled", "true")
            //show publish/receive debug logs of the cache message bus in the dashboard
            .WithEnvironment("Logging__LogLevel__Grand.Infrastructure.Caching.Redis", "Debug")
            .WithEnvironment("Redis__RedisPubSubChannel", "grandnode-cache")
            .WithEnvironment("Redis__RedisPubSubConnectionString", redis.Resource.ConnectionStringExpression);
    }
}
