using Aspire.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var mongo = builder.AddMongoDB("mongo").WithLifetime(ContainerLifetime.Persistent);
var mongodb = mongo.AddDatabase("Mongodb");

var redis = builder.AddRedis("redis").WithLifetime(ContainerLifetime.Persistent);

builder.ConfigureGrandWebProject(mongodb, redis);

await builder.Build().RunAsync();
