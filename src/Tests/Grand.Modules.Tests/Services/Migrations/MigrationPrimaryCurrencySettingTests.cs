using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Configuration;
using Grand.Domain.Directory;
using Grand.Module.Migration.Migrations._2._4;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Text.Json;

namespace Grand.Modules.Tests.Services.Migrations;

[TestClass]
public class MigrationPrimaryCurrencySettingTests
{
    private const string CurrencySettingsName = "currencysettings";
    private const string PrimaryCurrencySettingsName = "primarycurrencysettings";

    private MigrationPrimaryCurrencySetting _migration;
    private IRepository<Setting> _repository;

    [TestInitialize]
    public void Setup()
    {
        _repository = new MongoDBRepositoryTest<Setting>();
        _migration = new MigrationPrimaryCurrencySetting();

        var services = new ServiceCollection();
        services.AddSingleton(_repository);
        services.AddSingleton(Mock.Of<ILogger<MigrationPrimaryCurrencySetting>>());
        _serviceProvider = services.BuildServiceProvider();
    }

    private IServiceProvider _serviceProvider;

    private void GivenCurrencySettings(string storeId, string primaryStoreCurrencyId)
    {
        _repository.Insert(new Setting {
            Name = CurrencySettingsName,
            StoreId = storeId,
            Metadata = JsonSerializer.Serialize(new {
                PrimaryStoreCurrencyId = primaryStoreCurrencyId,
                PrimaryExchangeRateCurrencyId = "exchange-currency",
                ActiveExchangeRateProviderSystemName = "CurrencyExchange.MoneyConverter",
                AutoUpdateEnabled = false
            })
        });
    }

    private string PrimaryCurrencyIdOf(string storeId)
    {
        var setting = _repository.Table
            .FirstOrDefault(x => x.Name == PrimaryCurrencySettingsName && x.StoreId == storeId);

        return setting == null
            ? null
            : JsonSerializer.Deserialize<PrimaryCurrencySettings>(setting.Metadata)!.CurrencyId;
    }

    [TestMethod]
    public void UpgradeProcess_CopiesTheGlobalPrimaryCurrencyIntoItsOwnSetting()
    {
        GivenCurrencySettings(string.Empty, "currency-1");

        var result = _migration.UpgradeProcess(_serviceProvider);

        Assert.IsTrue(result);
        Assert.AreEqual("currency-1", PrimaryCurrencyIdOf(string.Empty));
    }

    [TestMethod]
    public void UpgradeProcess_CopiesStoreScopedCurrencySettingsSeparately()
    {
        GivenCurrencySettings(string.Empty, "currency-1");
        GivenCurrencySettings("store-1", "currency-2");

        _migration.UpgradeProcess(_serviceProvider);

        Assert.AreEqual("currency-1", PrimaryCurrencyIdOf(string.Empty));
        Assert.AreEqual("currency-2", PrimaryCurrencyIdOf("store-1"));
    }

    [TestMethod]
    public void UpgradeProcess_RunTwice_DoesNotDuplicateOrOverwrite()
    {
        GivenCurrencySettings(string.Empty, "currency-1");
        _migration.UpgradeProcess(_serviceProvider);

        _migration.UpgradeProcess(_serviceProvider);

        Assert.AreEqual(1,
            _repository.Table.Count(x => x.Name == PrimaryCurrencySettingsName && x.StoreId == string.Empty));
    }

    [TestMethod]
    public void UpgradeProcess_NoPrimaryCurrencyStored_WritesNothing()
    {
        GivenCurrencySettings(string.Empty, null);

        var result = _migration.UpgradeProcess(_serviceProvider);

        Assert.IsTrue(result);
        Assert.IsNull(PrimaryCurrencyIdOf(string.Empty));
    }

    [TestMethod]
    public void UpgradeProcess_NoCurrencySettingsAtAll_WritesNothing()
    {
        var result = _migration.UpgradeProcess(_serviceProvider);

        Assert.IsTrue(result);
        Assert.IsEmpty(_repository.Table.Where(x => x.Name == PrimaryCurrencySettingsName).ToList());
    }
}
