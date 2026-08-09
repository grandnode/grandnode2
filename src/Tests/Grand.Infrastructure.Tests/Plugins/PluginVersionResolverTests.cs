using Grand.Infrastructure.Plugins;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Grand.Infrastructure.Tests.Plugins;

[TestClass]
public class PluginVersionResolverTests
{
    /// <summary>
    ///     Stands in for a plugin assembly: it is compiled against the same Grand.Infrastructure
    ///     as any real plugin, so its reference version is the one a plugin would carry.
    /// </summary>
    private static readonly Assembly PluginLikeAssembly = typeof(PluginVersionResolverTests).Assembly;

    [TestMethod]
    public void ResolveSupportedVersion_AssemblyBuiltAgainstCurrentCore_MatchesSupportedPluginVersion()
    {
        var result = PluginVersionResolver.ResolveSupportedVersion(PluginLikeAssembly, null);

        Assert.AreEqual(GrandVersion.SupportedPluginVersion, result);
    }

    [TestMethod]
    public void ResolveSupportedVersion_DeclaredVersion_WinsOverAssemblyReference()
    {
        var result = PluginVersionResolver.ResolveSupportedVersion(PluginLikeAssembly, "1.0");

        Assert.AreEqual("1.0", result);
        Assert.AreNotEqual(GrandVersion.SupportedPluginVersion, result,
            "A plugin declaring an old version must stay distinguishable from the current one - " +
            "this is the comparison the compatibility gate relies on.");
    }

    [TestMethod]
    public void ResolveSupportedVersion_DeclaredVersionIsWhitespace_FallsBackToAssemblyReference()
    {
        var result = PluginVersionResolver.ResolveSupportedVersion(PluginLikeAssembly, "   ");

        Assert.AreEqual(GrandVersion.SupportedPluginVersion, result);
    }

    [TestMethod]
    public void ResolveSupportedVersion_DeclaredVersionIsPadded_IsTrimmed()
    {
        var result = PluginVersionResolver.ResolveSupportedVersion(PluginLikeAssembly, " 2.3 ");

        Assert.AreEqual("2.3", result);
    }

    [TestMethod]
    public void ResolveSupportedVersion_AssemblyWithoutCoreReference_ReturnsNull()
    {
        //System.Private.CoreLib does not reference Grand.Infrastructure
        var result = PluginVersionResolver.ResolveSupportedVersion(typeof(object).Assembly, null);

        Assert.IsNull(result,
            "A plugin that cannot be tied to a core version must not be reported as compatible.");
    }

    [TestMethod]
    public void ResolveSupportedVersion_NoAssembly_ReturnsNull()
    {
        var result = PluginVersionResolver.ResolveSupportedVersion(null, null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void PluginInfoAttribute_DoesNotSelfAssignSupportedVersion()
    {
        //The attribute used to compute SupportedVersion in its constructor from
        //Assembly.GetExecutingAssembly() - always Grand.Infrastructure, never the plugin -
        //which made every plugin compare equal to GrandVersion.SupportedPluginVersion.
        var attribute = new PluginInfoAttribute();

        Assert.IsNull(attribute.SupportedVersion);
    }
}
