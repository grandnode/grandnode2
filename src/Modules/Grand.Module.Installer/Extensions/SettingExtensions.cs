using Grand.Data;
using Grand.Domain.Configuration;
using System.Text.Json;

namespace Grand.Module.Installer.Extensions;

public static class SettingExtensions
{
    public static async Task SaveSetting<T>(this IRepository<Setting> repository, T settings, string storeId = "") where T : ISettings, new()
    {
        var setting = new Setting {
            Name = typeof(T).Name.ToLowerInvariant(),
            Metadata = JsonSerializer.Serialize(settings),
            StoreId = storeId
        };
        await repository.InsertAsync(setting);

    }
}
