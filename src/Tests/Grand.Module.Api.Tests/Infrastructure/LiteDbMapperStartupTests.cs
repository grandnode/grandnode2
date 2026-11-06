using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Infrastructure;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Reflection;

namespace Grand.Module.Api.Tests.Infrastructure;

[TestClass]
public class LiteDbMapperStartupTests
{
    [TestMethod]
    public void ConfigureServices_OnLiteDb_BuildsTheMappersOfTheApiModels()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { ["Database:UseLiteDb"] = "true" })
            .Build();

        new LiteDbMapperStartup().ConfigureServices(new ServiceCollection(), configuration);

        var entities = typeof(BsonMapper).GetField("_entities", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var mapped = ((IDictionary)entities.GetValue(BsonMapper.Global)!).Keys.Cast<Type>().ToList();
        CollectionAssert.Contains(mapped, typeof(CategoryDto));
    }
}
