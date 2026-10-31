using Grand.Data;
using Grand.Domain;
using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.Startup;
using Grand.Infrastructure.Tests.Plugins;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Reflection;

namespace Grand.Infrastructure.Tests.Startup;

[TestClass]
public class StartupApplicationTests
{
    [TestMethod]
    [DoNotParallelize]
    public void ConfigureServices_OnLiteDb_BuildsTheMappersOfAnUninstalledPlugin()
    {
        //installing a plugin uses its entities on the running site, among requests reading the same mappers
        DataSettingsManager.Initialize(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.cfg"));
        var referencedPlugins = PluginManager.ReferencedPlugins;
        try
        {
            PluginManager.ReferencedPlugins = [
                new PluginInfo(null, typeof(SampleBasePlugin).Assembly, typeof(SampleBasePlugin)) {
                    SystemName = "SamplePlugin",
                    Installed = false
                }
            ];
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { ["Database:UseLiteDb"] = "true" })
                .Build();

            new StartupApplication().ConfigureServices(new ServiceCollection(), configuration);

            var entities = typeof(BsonMapper).GetField("_entities", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var mapped = ((IDictionary)entities.GetValue(BsonMapper.Global)!).Keys.Cast<Type>().ToList();
            CollectionAssert.Contains(mapped, typeof(SamplePluginEntity));
        }
        finally
        {
            PluginManager.ReferencedPlugins = referencedPlugins;
        }
    }

    private class SamplePluginEntity : BaseEntity;
}
