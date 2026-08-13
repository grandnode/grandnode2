using System.Reflection;
using Grand.Data;
using Grand.Web.Common.Infrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Infrastructure.HealthChecks;

[TestClass]
[DoNotParallelize]
public class StartupHealthCheckTests
{
    // DataSettingsManager caches DatabaseIsInstalled() on first call and its public ResetCache()
    // can only force the cached value to false, never back to true - see DataSettingsManager.cs.
    // Resetting the private static instance per test keeps "installed" and "not installed" cases
    // independent instead of depending on test execution order. [DoNotParallelize] on this class
    // stops these tests running concurrently with each other against that same process-wide state.
    private static readonly FieldInfo InstanceField = typeof(DataSettingsManager)
        .GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException(
            "DataSettingsManager._instance field not found - has the type changed?");

    private string _settingsPath = null!;

    [TestInitialize]
    public void Setup()
    {
        InstanceField.SetValue(null, null);
        _settingsPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
        DataSettingsManager.Initialize(_settingsPath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        InstanceField.SetValue(null, null);
        if (File.Exists(_settingsPath))
            File.Delete(_settingsPath);
    }

    private static Mock<IHostApplicationLifetime> MockLifetime(bool started)
    {
        var cts = new CancellationTokenSource();
        if (started) cts.Cancel();

        var lifetime = new Mock<IHostApplicationLifetime>();
        lifetime.Setup(l => l.ApplicationStarted).Returns(cts.Token);
        return lifetime;
    }

    [TestMethod]
    public async Task CheckHealthAsync_ApplicationNotStarted_ReturnsUnhealthy()
    {
        DataSettingsManager.Instance.LoadDataSettings(
            new DataSettings { ConnectionString = "mongodb://localhost/test", DbProvider = DbProvider.MongoDB });

        var check = new StartupHealthCheck(MockLifetime(started: false).Object);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
    }

    [TestMethod]
    public async Task CheckHealthAsync_StartedButDatabaseNotConfigured_ReturnsUnhealthy()
    {
        // no connection string loaded - DatabaseIsInstalled() evaluates to false on first call
        var check = new StartupHealthCheck(MockLifetime(started: true).Object);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
    }

    [TestMethod]
    public async Task CheckHealthAsync_StartedAndDatabaseConfigured_ReturnsHealthy()
    {
        DataSettingsManager.Instance.LoadDataSettings(
            new DataSettings { ConnectionString = "mongodb://localhost/test", DbProvider = DbProvider.MongoDB });

        var check = new StartupHealthCheck(MockLifetime(started: true).Object);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.AreEqual(HealthStatus.Healthy, result.Status);
    }
}
