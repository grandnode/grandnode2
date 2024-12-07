﻿using Grand.Data;
using Grand.SharedKernel.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestContext = NUnit.Framework.TestContext;

namespace Grand.Domain.Tests.Data;

[TestClass]
public class DataSettingsManagerTests
{
    public DataSettingsManagerTests()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, CommonPath.AppData, CommonPath.SettingsFile);
        DataSettingsManager.Initialize(path);
    }

    [TestInitialize]
    public void Setup()
    {
        DataSettingsManager.Instance.LoadDataSettings(null);

        /*if (File.Exists(CommonPath.SettingsPath))
            File.Delete(CommonPath.SettingsPath);*/
    }

    [TestMethod]
    public async Task SaveSettings_LoadSettings_Test()
    {
        await DataSettingsManager.Instance.SaveSettings(new DataSettings
            { ConnectionString = "connectionstring", DbProvider = DbProvider.MongoDB });
        var settings = DataSettingsManager.Instance.LoadSettings();
        Assert.IsNotNull(settings);
        Assert.IsTrue(DataSettingsManager.DatabaseIsInstalled());
        Assert.AreEqual("connectionstring", settings.ConnectionString);
    }


    [TestMethod]
    public async Task DatabaseIsInstalledTest_True()
    {
        await DataSettingsManager.Instance.SaveSettings(new DataSettings
            { ConnectionString = "connectionstring", DbProvider = DbProvider.MongoDB });
        Assert.IsTrue(DataSettingsManager.DatabaseIsInstalled());
    }
}