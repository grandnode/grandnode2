﻿using Grand.Infrastructure.Tests.Plugins;
using Grand.SharedKernel.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Plugins.Tests
{
    [TestClass()]
    public class BasePluginTests
    {
        SampleBasePlugin sampleBasePlugin;

        [TestInitialize()]
        public void Init()
        {
            CommonPath.BaseDirectory = NUnit.Framework.TestContext.CurrentContext.TestDirectory;
            if (File.Exists(CommonPath.InstalledPluginsFilePath))
                File.Delete(CommonPath.InstalledPluginsFilePath);

            sampleBasePlugin = new SampleBasePlugin();

        }
        [TestMethod()]
        public void ConfigurationUrlTest()
        {
            Assert.IsNotNull(sampleBasePlugin.ConfigurationUrl());
        }

        [TestMethod()]
        public async Task InstallTest()
        {
            await sampleBasePlugin.Install();
            var plugins = PluginExtensions.ParseInstalledPluginsFile(CommonPath.InstalledPluginsFilePath);
            Assert.IsNotNull(plugins);
            Assert.AreEqual("SamplePlugin", plugins.FirstOrDefault());
        }

        [TestMethod()]
        public async Task UninstallTest_WithInstall()
        {
            await sampleBasePlugin.Install();
            await sampleBasePlugin.Uninstall();
            var plugins = PluginExtensions.ParseInstalledPluginsFile(CommonPath.InstalledPluginsFilePath);
            Assert.AreEqual(0, plugins.Count());
        }

        [TestMethod()]
        public async Task UninstallTest()
        {
            await sampleBasePlugin.Uninstall();
            var plugins = PluginExtensions.ParseInstalledPluginsFile(CommonPath.InstalledPluginsFilePath);
            Assert.AreEqual(0, plugins.Count());
        }
    }
}