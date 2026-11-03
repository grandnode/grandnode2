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
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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

        var httpContext = new DefaultHttpContext();
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
            await _controller.Remove(SystemName);

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
