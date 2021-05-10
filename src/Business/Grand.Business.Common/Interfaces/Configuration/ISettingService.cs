using Grand.Domain.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Common.Interfaces.Configuration
{
    /// <summary>
    /// Setting service interface
    /// </summary>
    public partial interface ISettingService
    {

        /// <summary>
        /// Adds a setting
        /// </summary>
        /// <param name="setting">Setting</param>
        /// <param name="clearCache">A value indicating whether to clear cache after setting update</param>
        Task InsertSetting(Setting setting, bool clearCache = true);

        /// <summary>
        /// Update setting
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="clearCache"></param>
        /// <returns></returns>
        Task UpdateSetting(Setting setting, bool clearCache = true);

        /// <summary>
        /// Deletes a setting
        /// </summary>
        /// <param name="setting">Setting</param>
        Task DeleteSetting(Setting setting);

        /// <summary>
        /// Gets a setting by ident
        /// </summary>
        /// <param name="settingId">Setting ident</param>
        /// <returns>Setting</returns>
        Task<Setting> GetSettingById(string settingId);

        /// <summary>
        /// Get setting value by key
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="key">Key</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>Setting value</returns>
        T GetSettingByKey<T>(string key, T defaultValue = default, string storeId = "");

        /// <summary>
        /// Set setting value
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="clearCache">A value indicating whether to clear cache after setting update</param>
        Task SetSetting<T>(string key, T value, string storeId = "", bool clearCache = true);

        /// <summary>
        /// Gets all settings
        /// </summary>
        /// <returns>Settings</returns>
        IList<Setting> GetAllSettings();

        /// <summary>
        /// Load settings
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="storeId">Store identifier for which settigns should be loaded</param>
        T LoadSetting<T>(string storeId = "") where T : ISettings, new();

        /// <summary>
        /// Load settings
        /// </summary>
        /// <param name="type"></param>
        /// <param name="storeId"></param>
        /// <returns></returns>
        ISettings LoadSetting(Type type, string storeId = "");

        /// <summary>
        /// Save settings object
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="storeId">Store identifier</param>
        /// <param name="settings">Setting instance</param>
        Task SaveSetting<T>(T settings, string storeId = "") where T : ISettings, new();

        /// <summary>
        /// Delete all settings
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        Task DeleteSetting<T>() where T : ISettings, new();

        /// <summary>
        /// Clear cache
        /// </summary>
        Task ClearCache();
    }
}
