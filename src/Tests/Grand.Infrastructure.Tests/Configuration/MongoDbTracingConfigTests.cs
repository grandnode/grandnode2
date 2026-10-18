using Grand.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;

namespace Grand.Infrastructure.Tests.Configuration;

[TestClass]
public class MongoDbTracingConfigTests
{
    private const string ConnectionString = "mongodb://127.0.0.1:27017/grandnode";

    [TestMethod]
    public void ApplyTo_NotConfigured_LeavesTheDriverDefaultWithoutQueryText()
    {
        //Arrange
        var clientSettings = MongoClientSettings.FromConnectionString(ConnectionString);
        var defaultOptions = clientSettings.TracingOptions;
        //Act
        new MongoDbTracingConfig().ApplyTo(clientSettings);
        //Assert
        Assert.AreEqual(defaultOptions, clientSettings.TracingOptions);
        Assert.AreEqual(0, clientSettings.TracingOptions?.QueryTextMaxLength ?? 0);
    }

    [TestMethod]
    public void ApplyTo_QueryTextMaxLengthFromConfiguration_IsSetOnTheClient()
    {
        //Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> {
            ["Telemetry:Instrumentation:MongoDb:QueryTextMaxLength"] = "500"
        }).Build();
        var config = new MongoDbTracingConfig();
        configuration.GetSection(MongoDbTracingConfig.SectionName).Bind(config);
        var clientSettings = MongoClientSettings.FromConnectionString(ConnectionString);
        //Act
        config.ApplyTo(clientSettings);
        //Assert
        Assert.AreEqual(500, clientSettings.TracingOptions.QueryTextMaxLength);
        Assert.IsFalse(clientSettings.TracingOptions.Disabled);
    }

    [TestMethod]
    public void ApplyTo_DisabledFromConfiguration_TurnsTracingOffInTheDriver()
    {
        //Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> {
            ["Telemetry:Instrumentation:MongoDb:Enabled"] = "false",
            ["Telemetry:Instrumentation:MongoDb:QueryTextMaxLength"] = "500"
        }).Build();
        var config = new MongoDbTracingConfig();
        configuration.GetSection(MongoDbTracingConfig.SectionName).Bind(config);
        var clientSettings = MongoClientSettings.FromConnectionString(ConnectionString);
        //Act
        config.ApplyTo(clientSettings);
        //Assert
        Assert.IsTrue(clientSettings.TracingOptions.Disabled);
    }

    [TestMethod]
    public void ApplyTo_NegativeQueryTextMaxLength_Throws()
    {
        //Arrange
        var config = new MongoDbTracingConfig { QueryTextMaxLength = -1 };
        //Act and Assert
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            config.ApplyTo(MongoClientSettings.FromConnectionString(ConnectionString)));
    }
}
