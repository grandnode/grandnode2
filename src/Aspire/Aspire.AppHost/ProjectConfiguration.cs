namespace Aspire.AppHost;

public static class ProjectConfiguration
{
    public static void ConfigureGrandWebProject(this IDistributedApplicationBuilder builder, IResourceBuilder<MongoDBDatabaseResource> mongodb)
    {
        builder
            .AddProject<Projects.Grand_Web>("grand-web")
            .WithHttpEndpoint(80, name: "front")
            .WithReference(mongodb)
            .WaitFor(mongodb);
    }
}