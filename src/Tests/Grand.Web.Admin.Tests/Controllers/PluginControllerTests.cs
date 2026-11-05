using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Plugins;
using Grand.Web.Admin.Controllers;
using Grand.Web.Common.Audit;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Reflection;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class PluginControllerTests
{
    private const string SystemName = "Tests.Plugin";

    private string _contentRoot;
    private ExtensionsConfig _extConfig;
    private Mock<IHostApplicationLifetime> _applicationLifetime;
    private Mock<IAuditEventRecorder> _auditEventRecorder;
    private Mock<IPlugin> _plugin;
    private Mock<IServiceProvider> _serviceProvider;
    private PluginController _controller;

    [TestInitialize]
    public void Setup()
    {
        _contentRoot = Path.Combine(Path.GetTempPath(), "grand-plugin-controller-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_contentRoot);

        _extConfig = new ExtensionsConfig();
        _applicationLifetime = new Mock<IHostApplicationLifetime>();
        _auditEventRecorder = new Mock<IAuditEventRecorder>();
        _plugin = new Mock<IPlugin>();
        _serviceProvider = new Mock<IServiceProvider>();
        _serviceProvider.Setup(x => x.GetService(typeof(IPlugin))).Returns(_plugin.Object);

        var translationService = new Mock<IRequestTranslationService>();
        translationService.Setup(x => x.GetResource(It.IsAny<string>())).Returns((string key) => key);

        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(new Customer { Email = "admin@example.com" });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);

        var hostEnvironment = new Mock<IWebHostEnvironment>();
        hostEnvironment.Setup(x => x.ContentRootPath).Returns(_contentRoot);

        _controller = new PluginController(
            translationService.Object,
            new Mock<ILogger<PluginController>>().Object,
            _applicationLifetime.Object,
            contextAccessor.Object,
            _serviceProvider.Object,
            new Mock<IEnumTranslationService>().Object,
            hostEnvironment.Object,
            _extConfig,
            _auditEventRecorder.Object);

        //RedirectToAction builds its URL helper, and an error notification logs, through the request services
        var urlHelperFactory = new Mock<IUrlHelperFactory>();
        urlHelperFactory.Setup(x => x.GetUrlHelper(It.IsAny<ActionContext>())).Returns(new Mock<IUrlHelper>().Object);
        var httpContext = new DefaultHttpContext {
            RequestServices = new ServiceCollection()
                .AddLogging()
                .AddSingleton(urlHelperFactory.Object)
                .BuildServiceProvider()
        };
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Directory.Delete(_contentRoot, true);
    }

    [TestMethod]
    public void ReloadList_IsPostOnly()
    {
        var method = typeof(PluginController).GetMethod(nameof(PluginController.ReloadList));
        Assert.IsNotNull(method);
        Assert.IsNotNull(method.GetCustomAttribute<HttpPostAttribute>());
        Assert.IsNull(method.GetCustomAttribute<HttpGetAttribute>());
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task Install_DoesNotStopApplication()
    {
        await WithPlugins(async () =>
        {
            await _controller.Install(SystemName);

            _plugin.Verify(x => x.Install(), Times.Once);
            _applicationLifetime.Verify(x => x.StopApplication(), Times.Never);
        }, Plugin(false));
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task Uninstall_DoesNotStopApplication()
    {
        await WithPlugins(async () =>
        {
            await _controller.Uninstall(SystemName);

            _plugin.Verify(x => x.Uninstall(), Times.Once);
            _applicationLifetime.Verify(x => x.StopApplication(), Times.Never);
        }, Plugin(true));
    }

    [TestMethod]
    public async Task ReloadList_StopsApplicationAndAudits()
    {
        await _controller.ReloadList();

        _auditEventRecorder.Verify(x => x.Plugin("*", "restarted"), Times.Once);
        _applicationLifetime.Verify(x => x.StopApplication(), Times.Once);
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task Remove_InstalledPlugin_DoesNotDeleteFolder()
    {
        var folder = Path.Combine(_contentRoot, "Plugins", "Tests.Plugin");
        Directory.CreateDirectory(folder);
        var mainAssembly = Path.Combine(folder, "Tests.Plugin.dll");
        await File.WriteAllTextAsync(mainAssembly, "");
        var plugin = new PluginInfo(new FileInfo(mainAssembly), null, typeof(IPlugin)) {
            SystemName = SystemName,
            SupportedVersion = GrandVersion.SupportedPluginVersion,
            Installed = true
        };

        await WithPlugins(async () =>
        {
            await _controller.Remove(SystemName, "Tests.Plugin");

            Assert.IsTrue(Directory.Exists(folder));
            _applicationLifetime.Verify(x => x.StopApplication(), Times.Never);
        }, plugin);
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task Install_WhenInstalledPluginsConfigured_DoesNotInstall()
    {
        _extConfig.InstalledPlugins = "Other.Plugin";

        await WithPlugins(async () =>
        {
            await _controller.Install(SystemName);

            _plugin.Verify(x => x.Install(), Times.Never);
        }, Plugin(false));
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task Install_ShowsTheAfterRestartText()
    {
        await WithPlugins(async () =>
        {
            await _controller.Install(SystemName);

            CollectionAssert.Contains(Notifications("Success"), "Admin.Plugins.Installed.AfterRestart");
        }, Plugin(false));
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task Uninstall_PluginWithLoadError_MarksUninstalledWithoutInstance()
    {
        var plugin = BrokenPlugin(null);

        await WithInstalledFile(async installedFile =>
        {
            await WithPlugins(async () =>
            {
                await _controller.Uninstall(SystemName);

                CollectionAssert.DoesNotContain(PluginExtensions.ParseInstalledPluginsFile(installedFile).ToList(),
                    SystemName);
                _serviceProvider.Verify(x => x.GetService(It.IsAny<Type>()), Times.Never);
                _plugin.Verify(x => x.Uninstall(), Times.Never);
                _auditEventRecorder.Verify(x => x.Plugin(SystemName, "uninstalled"), Times.Once);
                CollectionAssert.Contains(Notifications("Warning"), "Admin.Plugins.Uninstalled.NotCleanedUp");
                _applicationLifetime.Verify(x => x.StopApplication(), Times.Never);
            }, plugin);
        });
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task Remove_AfterUninstallOfBrokenPlugin_DeletesFolder()
    {
        var folder = Path.Combine(_contentRoot, "Plugins", "Tests.Plugin");
        Directory.CreateDirectory(folder);
        var mainAssembly = Path.Combine(folder, "Tests.Plugin.dll");
        await File.WriteAllTextAsync(mainAssembly, "");
        var plugin = BrokenPlugin(new FileInfo(mainAssembly));

        await WithInstalledFile(async _ =>
        {
            await WithPlugins(async () =>
            {
                await _controller.Uninstall(SystemName);
                await _controller.Remove(SystemName, "Tests.Plugin");

                Assert.IsFalse(Directory.Exists(folder));
                _applicationLifetime.Verify(x => x.StopApplication(), Times.Never);
            }, plugin);
        });
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task Remove_SecondFolderOfASystemName_DeletesOnlyThatFolder()
    {
        var working = Path.Combine(_contentRoot, "Plugins", "Tests.Plugin");
        Directory.CreateDirectory(working);
        var mainAssembly = Path.Combine(working, "Tests.Plugin.dll");
        await File.WriteAllTextAsync(mainAssembly, "");
        var copy = Directory.CreateDirectory(Path.Combine(_contentRoot, "Plugins", "Tests.Plugin.Copy")).FullName;
        var plugin = new PluginInfo(new FileInfo(mainAssembly), null, typeof(IPlugin)) {
            SystemName = SystemName,
            SupportedVersion = GrandVersion.SupportedPluginVersion
        };
        var duplicate = PluginLoader.DescribeProblem(
            new PluginLoadProblem("Tests.Plugin.Copy", "already used", [SystemName]), []).Single();

        await WithPlugins(async () =>
        {
            await _controller.Remove(SystemName, "Tests.Plugin.Copy");

            Assert.IsFalse(Directory.Exists(copy));
            Assert.IsTrue(Directory.Exists(working));
        }, plugin, duplicate);
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task Remove_FolderWhosePluginWasNotIdentified_DeletesIt()
    {
        var folder = Directory.CreateDirectory(Path.Combine(_contentRoot, "Plugins", "Broken")).FullName;
        var entry = PluginLoader.DescribeProblem(new PluginLoadProblem("Broken", "has no SystemName"), []).Single();

        await WithPlugins(async () =>
        {
            await _controller.Remove("Broken", "Broken");

            Assert.IsFalse(Directory.Exists(folder));
            CollectionAssert.Contains(Notifications("Success"), "Admin.Plugins.Removed.AfterRestart");
        }, entry);
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task Install_AfterRemoveBeforeRestart_Refuses()
    {
        await WithLoadedFolder(false, folder => Directory.Delete(folder, true), async installedFile =>
        {
            await _controller.Install(SystemName);

            _plugin.Verify(x => x.Install(), Times.Never);
            Assert.IsEmpty(PluginExtensions.ParseInstalledPluginsFile(installedFile));
            CollectionAssert.Contains(Notifications("Error"), "Admin.Plugins.RestartFirst");
        });
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task Install_AfterUpdateBeforeRestart_Refuses()
    {
        await WithLoadedFolder(false, ReplaceMainAssembly, async installedFile =>
        {
            await _controller.Install(SystemName);

            _plugin.Verify(x => x.Install(), Times.Never);
            Assert.IsEmpty(PluginExtensions.ParseInstalledPluginsFile(installedFile));
            CollectionAssert.Contains(Notifications("Error"), "Admin.Plugins.RestartFirst");
        });
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task Uninstall_AfterUpdateBeforeRestart_Refuses()
    {
        await WithLoadedFolder(true, ReplaceMainAssembly, async installedFile =>
        {
            await _controller.Uninstall(SystemName);

            _plugin.Verify(x => x.Uninstall(), Times.Never);
            CollectionAssert.Contains(PluginExtensions.ParseInstalledPluginsFile(installedFile).ToList(), SystemName);
            CollectionAssert.Contains(Notifications("Error"), "Admin.Plugins.RestartFirst");
        });
    }

    /// <summary>
    ///     Runs with the plugin loaded from its folder as PluginManager.Load records it, after
    ///     <paramref name="changeOnDisk" /> changed that folder
    /// </summary>
    private async Task WithLoadedFolder(bool installed, Action<string> changeOnDisk, Func<string, Task> test)
    {
        var pluginsFolder = new DirectoryInfo(Path.Combine(_contentRoot, "Plugins"));
        var folder = Directory.CreateDirectory(Path.Combine(pluginsFolder.FullName, "Tests.Plugin"));
        var mainAssembly = Path.Combine(folder.FullName, "Tests.Plugin.dll");
        await File.WriteAllTextAsync(mainAssembly, "version 1");
        var manifest = new PluginManifest(SystemName, SystemName, "Misc", "Tests", "1.0",
            GrandVersion.SupportedPluginVersion);
        var scan = new PluginScanResult(
            [new PluginCandidate(folder, mainAssembly, manifest, [mainAssembly], [SystemName])], [], []);
        var plugin = new PluginInfo(new FileInfo(mainAssembly), null, typeof(IPlugin)) {
            SystemName = SystemName,
            SupportedVersion = GrandVersion.SupportedPluginVersion,
            Installed = installed
        };

        await WithInstalledFile(async installedFile =>
        {
            var loadSnapshot = PluginManager.LoadSnapshot;
            try
            {
                PluginManager.LoadSnapshot = PluginLoadSnapshot.Capture(pluginsFolder, scan,
                    PluginExtensions.ParseInstalledPluginsFile(installedFile), installedFile, null);
                changeOnDisk(folder.FullName);

                await WithPlugins(() => test(installedFile), plugin);
            }
            finally
            {
                PluginManager.LoadSnapshot = loadSnapshot;
            }
        }, installed ? [SystemName] : []);
    }

    private static void ReplaceMainAssembly(string folder)
    {
        File.WriteAllText(Path.Combine(folder, "Tests.Plugin.dll"), "version 2, a different build");
    }

    private List<string> Notifications(string type)
    {
        return _controller.TempData[$"grand.notifications.{type}"] as List<string> ?? [];
    }

    /// <summary>
    ///     An installed plugin as loading leaves one that failed: with its load error and without a plugin type,
    ///     so no instance can be created
    /// </summary>
    private static PluginInfo BrokenPlugin(FileInfo mainAssembly)
    {
        return new PluginInfo(mainAssembly, null, null) {
            SystemName = SystemName,
            SupportedVersion = GrandVersion.SupportedPluginVersion,
            Installed = true,
            LoadError = "Could not load file or assembly 'Tests.Plugin.Dependency'."
        };
    }

    /// <summary>
    ///     Runs with the installed-plugins file listing <paramref name="installed" /> (by default the plugin),
    ///     and removes it afterwards
    /// </summary>
    private async Task WithInstalledFile(Func<string, Task> test, string[] installed = null)
    {
        //initialized once per process; a later call keeps the first path
        PluginPaths.Initialize(Path.Combine(_contentRoot, "App_Data", "InstalledPlugins.cfg"));
        var installedFile = PluginPaths.Instance.InstalledPluginsFile;
        Directory.CreateDirectory(Path.GetDirectoryName(installedFile)!);
        await File.WriteAllTextAsync(installedFile,
            System.Text.Json.JsonSerializer.Serialize(installed ?? [SystemName]));
        try
        {
            await test(installedFile);
        }
        finally
        {
            File.Delete(installedFile);
        }
    }

    private static PluginInfo Plugin(bool installed)
    {
        return new PluginInfo {
            SystemName = SystemName,
            SupportedVersion = GrandVersion.SupportedPluginVersion,
            PluginType = typeof(IPlugin),
            Installed = installed
        };
    }

    private static async Task WithPlugins(Func<Task> test, params PluginInfo[] plugins)
    {
        var referencedPlugins = PluginManager.ReferencedPlugins;
        try
        {
            PluginManager.ReferencedPlugins = plugins;
            await test();
        }
        finally
        {
            PluginManager.ReferencedPlugins = referencedPlugins;
        }
    }
}
