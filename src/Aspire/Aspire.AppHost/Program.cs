using Aspire.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var mongo = builder.AddMongoDB("mongo").WithLifetime(ContainerLifetime.Persistent);
var mongodb = mongo.AddDatabase("Mongodb");
builder.ConfigureGrandWebProject(mongodb);

builder.Build().Run();
