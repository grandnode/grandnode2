using System.Text.Json;

namespace Grand.Domain.Configuration;

public static class SettingExtensions
{
    public static Setting CreateSetting<T>(T settings, string storeId = "") where T : ISettings, new()
    {
        var setting = new Setting {
            Name = typeof(T).Name.ToLowerInvariant(),
            Metadata = JsonSerializer.Serialize(settings),
            StoreId = storeId
        };
        return setting;
    }
}
