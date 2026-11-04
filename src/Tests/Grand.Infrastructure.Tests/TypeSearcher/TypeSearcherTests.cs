using Grand.Domain.Configuration;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Mapper;
using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.Startup;
using Grand.Infrastructure.Tests.Plugins;
using Grand.Infrastructure.Tests.Plugins.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Runtime.Loader;

namespace Grand.Infrastructure.Tests.TypeSearcher;

[TestClass]
public class TypeSearcherTests
{
    private readonly TypeSearch.TypeSearcher _typeSearcher = new();

    [TestMethod]
    public void GetAssembliesTest()
    {
        Assert.IsTrue(_typeSearcher.GetAssemblies().Any());
    }

    [TestMethod]
    public void ClassesOfTypeTest_onlyConcreteClasses_false()
    {
        Assert.IsTrue(_typeSearcher.ClassesOfType<ICacheBase>().Any());
    }

    [TestMethod]
    public void ClassesOfTypeTest_onlyConcreteClasses_true()
    {
        Assert.IsFalse(_typeSearcher.ClassesOfType<IWorkContext>().Any());
    }

    [TestMethod]
    [DoNotParallelize]
    public void GetAssemblies_ExcludesUninstalledPluginAssembly()
    {
        var referencedPlugins = PluginManager.ReferencedPlugins;
        try
        {
            PluginManager.ReferencedPlugins = [SamplePluginInfo(false)];

            Assert.DoesNotContain(typeof(SampleBasePlugin).Assembly, _typeSearcher.GetAssemblies());
            Assert.DoesNotContain(typeof(SampleBasePlugin), _typeSearcher.ClassesOfType<IPlugin>());
            Assert.IsTrue(_typeSearcher.ClassesOfType<ICacheBase>().Any());
        }
        finally
        {
            PluginManager.ReferencedPlugins = referencedPlugins;
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public void GetAssemblies_KeepsInstalledPluginAssembly()
    {
        var referencedPlugins = PluginManager.ReferencedPlugins;
        try
        {
            PluginManager.ReferencedPlugins = [SamplePluginInfo(true)];

            Assert.Contains(typeof(SampleBasePlugin).Assembly, _typeSearcher.GetAssemblies());
            Assert.Contains(typeof(SampleBasePlugin), _typeSearcher.ClassesOfType<IPlugin>());
        }
        finally
        {
            PluginManager.ReferencedPlugins = referencedPlugins;
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public void GetAssemblies_NothingChanged_ReturnsTheSameList()
    {
        var first = _typeSearcher.GetAssemblies();

        Assert.AreSame(first, new TypeSearch.TypeSearcher().GetAssemblies());
    }

    [TestMethod]
    [DoNotParallelize]
    public void GetAssemblies_AfterAssemblyLoaded_IncludesIt()
    {
        const string name = "Grand.Infrastructure.Tests.LateLoaded";
        var dir = Path.Combine(Path.GetTempPath(), "grand-typesearcher-" + Guid.NewGuid().ToString("N"));
        try
        {
            var path = TestPluginBuilder.Compile(dir, name, """
                [assembly: System.Reflection.AssemblyProduct("grandnode")]
                public class LateLoadedMarker { }
                """);

            Assert.DoesNotContain(x => x.GetName().Name == name, _typeSearcher.GetAssemblies());

            // The type cache keeps the assembly reachable, so the context is never unloaded; the
            // fixture holds one inert class and implements nothing a scan looks for.
            var context = new AssemblyLoadContext("late-loaded", true);
            using var stream = new MemoryStream(File.ReadAllBytes(path));
            var loaded = context.LoadFromStream(stream);

            Assert.Contains(loaded, _typeSearcher.GetAssemblies());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public void GetAssemblies_AfterReferencedPluginsReplaced_Recomputes()
    {
        var referencedPlugins = PluginManager.ReferencedPlugins;
        try
        {
            PluginManager.ReferencedPlugins = [SamplePluginInfo(true)];
            Assert.Contains(typeof(SampleBasePlugin).Assembly, _typeSearcher.GetAssemblies());

            PluginManager.ReferencedPlugins = [SamplePluginInfo(false)];
            Assert.DoesNotContain(typeof(SampleBasePlugin).Assembly, _typeSearcher.GetAssemblies());

            PluginManager.ReferencedPlugins = [SamplePluginInfo(true)];
            Assert.Contains(typeof(SampleBasePlugin).Assembly, _typeSearcher.GetAssemblies());
        }
        finally
        {
            PluginManager.ReferencedPlugins = referencedPlugins;
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public void ClassesOfType_SameResultAsUncachedScan()
    {
        foreach (var type in new[] { typeof(IStartupApplication), typeof(ISettings), typeof(IAutoMapperProfile) })
        {
            var expected = UncachedScan(type, _typeSearcher.GetAssemblies());

            CollectionAssert.AreEquivalent(expected, _typeSearcher.ClassesOfType(type).ToList());
            CollectionAssert.AreEquivalent(expected, _typeSearcher.ClassesOfType(type).ToList());
        }
    }

    private static List<Type> UncachedScan(Type assignTypeFrom, IEnumerable<Assembly> assemblies)
    {
        return assemblies.SelectMany(a => a.GetTypes())
            .Where(t => assignTypeFrom.IsAssignableFrom(t) && !t.IsInterface && t.IsClass && !t.IsAbstract)
            .ToList();
    }

    private static PluginInfo SamplePluginInfo(bool installed)
    {
        return new PluginInfo(null, typeof(SampleBasePlugin).Assembly, typeof(SampleBasePlugin)) {
            SystemName = "SamplePlugin",
            Installed = installed
        };
    }
}