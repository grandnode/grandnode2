using Grand.Infrastructure.Modules;
using Grand.Infrastructure.Tests.Plugins.Fixtures;
using Grand.Mediator;
using Grand.SharedKernel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Reflection;
using System.Runtime.Loader;

namespace Grand.Infrastructure.Tests.Modules;

[TestClass]
[DoNotParallelize]
public class ModuleLoaderTests
{
    private static readonly string MediatorAssemblyPath = typeof(IRequestHandler<,>).Assembly.Location;

    private PluginFolderFixture _fixture;
    private CapturingLoggerProvider _logs;

    private string ContentRoot => Path.GetDirectoryName(_fixture.PluginsFolder)!;

    [TestInitialize]
    public void Init()
    {
        _fixture = new PluginFolderFixture();
        _logs = new CapturingLoggerProvider();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fixture.Dispose();
    }

    [TestMethod]
    public void LoadModules_HostAssemblyCopyInModuleFolder_UsesHostCopy()
    {
        File.Copy(MediatorAssemblyPath, Path.Combine(ModuleFolder("Grand.Module.Fake"), "Grand.Mediator.dll"));
        AddModule("Grand.Module.Fake", """
            public class Ping : Grand.Mediator.IRequest<string> { }
            public class PingHandler : Grand.Mediator.IRequestHandler<Ping, string>
            {
                public System.Threading.Tasks.Task<string> Handle(Ping request,
                    System.Threading.CancellationToken cancellationToken) => System.Threading.Tasks.Task.FromResult("pong");
            }
            """, MediatorAssemblyPath);

        Load(false, "Grand.Module.Fake");

        var handler = _fixture.Loaded("Grand.Module.Fake").GetType("PingHandler", true)!;
        Assert.IsTrue(handler.GetInterfaces()
            .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)));
        Assert.IsFalse(LoadedByModules().Any(x => x.GetName().Name == "Grand.Mediator"));
    }

    [TestMethod]
    public void LoadModules_OnlyModuleAssemblyIsApplicationPart()
    {
        //a private library of the module, loaded when the module's code needs it
        var lib = TestPluginBuilder.Compile(ModuleFolder("Grand.Module.Parts"), "Parts.Lib",
            "namespace PartsLib { public static class Lib { public static string Value() => \"from lib\"; } }");
        AddModule("Grand.Module.Parts", """
            public class PartsEndpoint : Microsoft.AspNetCore.Mvc.ControllerBase { }
            public static class Entry { public static string Run() => PartsLib.Lib.Value(); }
            """, lib);

        var mvcCoreBuilder = Load(false, "Grand.Module.Parts");

        var module = _fixture.Loaded("Grand.Module.Parts");
        CollectionAssert.AreEqual(new[] { module }, ModuleParts(mvcCoreBuilder));
        Assert.AreEqual("from lib", module.GetType("Entry", true)!.GetMethod("Run")!.Invoke(null, null));
    }

    [TestMethod]
    public void LoadModules_ModuleThatCannotLoad_IsLoggedCriticalAndLeftOut()
    {
        //its base class is in an assembly it does not ship
        var missing = _fixture.CompileElsewhere("Broken.Missing", "namespace Missing { public class Base { } }");
        //it uses GrandNode, so a type scan would include it
        AddModule("Grand.Module.Broken", """
            public class Derived : Missing.Base { }
            public class Startup { public Grand.Infrastructure.Plugins.IPlugin Plugin; }
            """, missing);
        AddModule("Grand.Module.Healthy", "public class Healthy { }");

        var mvcCoreBuilder = Load(false, "Grand.Module.Broken", "Grand.Module.Healthy");

        Assert.IsTrue(_logs.Entries.Any(x =>
            x.Level == LogLevel.Critical && x.Message.Contains("Grand.Module.Broken") &&
            x.Message.Contains("Broken.Missing")));
        var parts = ModuleParts(mvcCoreBuilder);
        Assert.IsFalse(parts.Any(x => x.GetName().Name == "Grand.Module.Broken"));
        Assert.IsTrue(parts.Any(x => x.GetName().Name == "Grand.Module.Healthy"));
        //its types would fail every scan
        Assert.IsFalse(new Infrastructure.TypeSearch.TypeSearcher().GetAssemblies().Any(x => x.GetName().Name == "Grand.Module.Broken"));
    }

    [TestMethod]
    public void LoadModules_ModuleThatCannotLoad_FailOnPluginLoadError_Throws()
    {
        var missing = _fixture.CompileElsewhere("Broken.Missing", "namespace Missing { public class Base { } }");
        AddModule("Grand.Module.Broken", "public class Derived : Missing.Base { }", missing);

        var ex = Assert.Throws<GrandException>(() => Load(true, "Grand.Module.Broken"));
        Assert.Contains("Grand.Module.Broken", ex.Message);
    }

    [TestMethod]
    public void LoadModules_ModuleBuiltAgainstNewerHostAssembly_FailsAtStartup()
    {
        //the host's copy wins, and it is older than the one the module was built against; used only in a method
        //body, so without the check the first request running it would fail
        var newer = TestPluginBuilder.Compile(Path.Combine(ContentRoot, "newer"), "Grand.Mediator",
            "namespace Grand.Mediator { public interface INewer { } }", new Version(99, 0, 0, 0));
        File.Copy(newer, Path.Combine(ModuleFolder("Grand.Module.Newer"), "Grand.Mediator.dll"));
        CompileAgainst(ModuleFolder("Grand.Module.Newer"), "Grand.Module.Newer",
            "public static class Uses { public static object M() => typeof(Grand.Mediator.INewer); }", newer);

        var mvcCoreBuilder = Load(false, "Grand.Module.Newer");

        Assert.IsTrue(_logs.Entries.Any(x =>
            x.Level == LogLevel.Critical && x.Message.Contains("Grand.Module.Newer") &&
            x.Message.Contains("Grand.Mediator")));
        Assert.IsFalse(ModuleParts(mvcCoreBuilder).Any());
    }

    private string ModuleFolder(string moduleName)
    {
        var folder = Path.Combine(ContentRoot, "Modules", moduleName);
        Directory.CreateDirectory(folder);
        return folder;
    }

    private string AddModule(string moduleName, string source, params string[] references)
    {
        return TestPluginBuilder.Compile(ModuleFolder(moduleName), moduleName, source, null,
            [PluginFolderFixture.CoreAssemblyPath, .. references]);
    }

    /// <summary>
    ///     Compiles against <paramref name="replacement" /> in place of the platform assembly of the same name
    /// </summary>
    private static void CompileAgainst(string outputDir, string assemblyName, string source, string replacement)
    {
        var replaced = Path.GetFileName(replacement);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(x => !string.Equals(Path.GetFileName(x), replaced, StringComparison.OrdinalIgnoreCase))
            .Append(replacement)
            .Select(x => MetadataReference.CreateFromFile(x));
        var result = CSharpCompilation.Create(assemblyName, [CSharpSyntaxTree.ParseText(source)], references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .Emit(Path.Combine(outputDir, assemblyName + ".dll"));
        Assert.IsTrue(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
    }

    private IMvcCoreBuilder Load(bool failOnPluginLoadError, params string[] modules)
    {
        var settings = modules.ToDictionary(x => "FeatureManagement:" + x, _ => "true");
        settings["Extensions:FailOnPluginLoadError"] = failOnPluginLoadError.ToString();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings!).Build();
        var hostEnvironment = new Mock<IWebHostEnvironment>();
        hostEnvironment.Setup(x => x.ContentRootPath).Returns(ContentRoot);
        var mvcCoreBuilder = new ServiceCollection().AddMvcCore();

        using var loggerFactory = LoggerFactory.Create(x => x.AddProvider(_logs));
        ModuleLoader.LoadModules(mvcCoreBuilder, configuration, hostEnvironment.Object, _fixture.CreateContext,
            loggerFactory);
        return mvcCoreBuilder;
    }

    private IEnumerable<Assembly> LoadedByModules() => _fixture.Contexts.SelectMany(x => x.Assemblies);

    /// <summary>
    ///     The application parts whose assembly a module context loaded
    /// </summary>
    private List<Assembly> ModuleParts(IMvcCoreBuilder mvcCoreBuilder)
    {
        var contexts = _fixture.Contexts;
        return mvcCoreBuilder.PartManager.ApplicationParts.OfType<AssemblyPart>()
            .Select(x => x.Assembly)
            .Where(x => contexts.Contains(AssemblyLoadContext.GetLoadContext(x)))
            .ToList();
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly List<(LogLevel Level, string Message)> _entries = [];

        public IReadOnlyList<(LogLevel Level, string Message)> Entries
        {
            get
            {
                lock (_entries)
                {
                    return _entries.ToList();
                }
            }
        }

        public ILogger CreateLogger(string categoryName) => new Logger(this);

        public void Dispose()
        {
        }

        private sealed class Logger(CapturingLoggerProvider provider) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                lock (provider._entries)
                {
                    provider._entries.Add((logLevel, formatter(state, exception)));
                }
            }
        }
    }
}
