using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Data;
using Grand.Domain.Configuration;
using Grand.Domain.Media;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Grand.Business.System.Services.Migrations._1._1;

public class MigrationUpdateMediaSettings : IMigration
{
    public int Priority => 0;
    public DbVersion Version => new(1, 1);
    public Guid Identity => new("899F6A9F-99BA-41C1-8E1F-9AE63A78E531");
    public string Name => "Update media settings - add new settings storage settings";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="database"></param>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<Setting>>();
        var mediaSettings = repository.Table.FirstOrDefault(x => x.Name == "mediasettings");
        var settingService = serviceProvider.GetRequiredService<ISettingService>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationUpdateMediaSettings>>();

        try
        {
            if (mediaSettings != null)
            {
                var metadata = mediaSettings.Metadata;
                var settingsLogo = JsonSerializer.Deserialize<SettingsMedia>(metadata);
                if (settingsLogo != null)
                    settingService.SaveSetting(new StorageSettings { PictureStoreInDb = settingsLogo.StoreInDb })
                        .GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - MigrationUpdateMediaSettings");
        }

        return true;
    }

    private class SettingsMedia
    {
        public bool StoreInDb { get; set; }
    }
}