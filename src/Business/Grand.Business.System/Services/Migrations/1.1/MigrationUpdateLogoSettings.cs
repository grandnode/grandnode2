using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Data;
using Grand.Domain.Stores;
using Grand.Infrastructure.Migrations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Grand.Business.System.Services.Migrations._1._1
{
    public class MigrationUpdateLogoSettings : IMigration
    {

        public int Priority => 0;
        public DbVersion Version => new(1, 1);
        public Guid Identity => new("82A5E26A-7861-4C72-9D5E-AF95477CE1F7");
        public string Name => "Update logo settings - StoreInformationSettings";

        /// <summary>
        /// Upgrade process
        /// </summary>
        /// <param name="database"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
        {
            var repository = serviceProvider.GetRequiredService<IRepository<Domain.Configuration.Setting>>();
            var storeSettings = repository.Table.Where(x => x.Name == "storeinformationsettings");
            var settingService = serviceProvider.GetRequiredService<ISettingService>();
            var logService = serviceProvider.GetRequiredService<ILogger>();
            var hostingEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();

            //update logo settings
            try
            {
                foreach (var storeSetting in storeSettings)
                {
                    var metadata = storeSetting.Metadata;
                    var settingsLogo = JsonSerializer.Deserialize<SettingsLogo>(metadata);
                    if (settingsLogo != null && !string.IsNullOrEmpty(settingsLogo.LogoPicture))
                    {
                        var pictureService = serviceProvider.GetRequiredService<IPictureService>();
                        new FileExtensionContentTypeProvider().TryGetContentType(settingsLogo.LogoPicture, out var mimeType);

                        var path = Path.Combine(hostingEnvironment.WebRootPath, settingsLogo.LogoPicture);

                        var pictureId = pictureService.InsertPicture(File.ReadAllBytes(path), mimeType, "").GetAwaiter().GetResult();
                        var storeInformationSettings = settingService.LoadSetting<StoreInformationSettings>(storeSetting.StoreId);
                        storeInformationSettings.LogoPictureId = pictureId?.Id;
                        settingService.SaveSetting<StoreInformationSettings>(storeInformationSettings, storeSetting.StoreId).GetAwaiter().GetResult();
                    }
                }
            }
            catch(Exception ex)
            {
                logService.InsertLog(Domain.Logging.LogLevel.Error, "UpgradeProcess - MigrationUpdateSettings", ex.Message).GetAwaiter().GetResult();
            }
            return true;
        }

        private class SettingsLogo
        {
            public string LogoPicture { get; set; }
        }

    }
}
