using Grand.Infrastructure.Plugins;
using Grand.Web.AdminShared.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Grand.Web.Admin.Tests.Extensions;

[TestClass]
public class PluginExtensionsTests
{
    private DirectoryInfo _pluginDirectory;

    [TestInitialize]
    public void Setup()
    {
        //<temp>/<guid>/Plugins/Payments.Test/Payments.Test.dll - the logo url is built from the last two folders
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _pluginDirectory = Directory.CreateDirectory(Path.Combine(root, "Plugins", "Payments.Test"));
    }

    [TestCleanup]
    public void Cleanup()
    {
        _pluginDirectory.Parent!.Parent!.Delete(true);
    }

    private PluginInfo Plugin(params string[] files)
    {
        foreach (var file in files)
            File.WriteAllText(Path.Combine(_pluginDirectory.FullName, file), string.Empty);
        return new PluginInfo(new FileInfo(Path.Combine(_pluginDirectory.FullName, "Payments.Test.dll")), null, null);
    }

    /// <summary>
    ///     No scheme or host: the host the store has configured can differ from the one the panel
    ///     was opened on (another domain, a reverse proxy, http behind https).
    /// </summary>
    [TestMethod]
    public void GetLogoUrl_AtTheRoot_ReturnsAPathWithoutHost()
    {
        Assert.AreEqual("/Plugins/Payments.Test/logo.jpg", Plugin("logo.jpg").GetLogoUrl(""));
    }

    [TestMethod]
    public void GetLogoUrl_UnderAPathBase_PrefixesThePathBase()
    {
        Assert.AreEqual("/shop/Plugins/Payments.Test/logo.png", Plugin("logo.png").GetLogoUrl("/shop/"));
    }

    [TestMethod]
    public void GetLogoUrl_PrefersJpgOverPng()
    {
        Assert.AreEqual("/Plugins/Payments.Test/logo.jpg", Plugin("logo.png", "logo.jpg").GetLogoUrl(null));
    }

    [TestMethod]
    public void GetLogoUrl_WithoutALogo_ReturnsNull()
    {
        Assert.IsNull(Plugin().GetLogoUrl(""));
    }
}
