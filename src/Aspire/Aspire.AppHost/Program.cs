
var builder = DistributedApplication.CreateBuilder(args);


var mongo = builder.AddMongoDB("mongo")
                   .WithLifetime(ContainerLifetime.Persistent);

var mongodb = mongo.AddDatabase("Mongodb");

builder
    .AddProject<Projects.Grand_Web>("grand-web")
    .WithHttpEndpoint(80)
    .WithReference(mongodb)
    .WaitFor(mongodb);

builder.Build().Run();
