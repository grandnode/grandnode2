using Grand.Data;
using Grand.Domain.Configuration;

namespace Grand.Module.Installer.Extensions;

public static class SettingExtensions
{
    public static async Task SaveSetting<T>(this IRepository<Setting> repository, T settings, string storeId = "") where T : ISettings, new()
    {
        var setting = Domain.Configuration.SettingExtensions.CreateSetting<T>(settings, storeId);
        await repository.InsertAsync(setting);
    }
}
