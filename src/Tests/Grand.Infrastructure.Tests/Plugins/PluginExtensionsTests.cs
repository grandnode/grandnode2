﻿using Grand.Infrastructure.Tests.Plugins;
using Grand.SharedKernel.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Plugins.Tests
{
    [TestClass()]
    public class PluginExtensionsTests
    {
        SampleBasePlugin sampleBasePlugin;
        public PluginExtensionsTests()
        {
            CommonPath.BaseDirectory = NUnit.Framework.TestContext.CurrentContext.TestDirectory;
            sampleBasePlugin = new SampleBasePlugin();
        }

        [TestMethod()]
        public async Task ParseInstalledPluginsFileTest()
        {
            await sampleBasePlugin.Install();
            var plugins = PluginExtensions.ParseInstalledPluginsFile(CommonPath.InstalledPluginsFilePath);
            Assert.IsNotNull(plugins);
        }

        [TestMethod()]
        public async Task SaveInstalledPluginsFileTest()
        {
            await PluginExtensions.SaveInstalledPluginsFile(new List<string> { "plugin1", "plugin2" }, CommonPath.InstalledPluginsFilePath);
            var plugins = PluginExtensions.ParseInstalledPluginsFile(CommonPath.InstalledPluginsFilePath);
            Assert.AreEqual(2, plugins.Count());
        }
    }
}