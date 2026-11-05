using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Data;
using Grand.Domain.Configuration;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Grand.Business.Common.Services.Configuration;

/// <summary>
///     Setting manager
/// </summary>
public class SettingService : ISettingService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="cacheBase">Cache manager</param>
    /// <param name="settingRepository">Setting repository</param>
    public SettingService(ICacheBase cacheBase,
        IRepository<Setting> settingRepository)
    {
        _cacheBase = cacheBase;
        _settingRepository = settingRepository;
    }

    #endregion

    #region Utilities

    /// <summary>
    ///     Gets a setting by identifier
    /// </summary>
    /// <param name="name">Setting name</param>
    /// <returns>Setting</returns>
    private IList<Setting> GetSettingsByName(string name)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(name);
        return _settingRepository.Table.Where(x => x.Name == name.ToLowerInvariant()).ToList();
    }

    private static string StoreSettingsKey(string storeId)
    {
        return string.Format(CacheKey.SETTINGS_BY_STORE, storeId ?? "");
    }

    /// <summary>
    ///     Gets the settings of a store and the global ones, read with one query and cached together
    /// </summary>
    private Task<StoreSettings> GetStoreSettings(string storeId)
    {
        storeId ??= "";
        return _cacheBase.GetAsync(StoreSettingsKey(storeId), async () =>
            new StoreSettings(await _settingRepository.ToListAsync(StoreSettingsQuery(storeId)), storeId));
    }

    private IQueryable<Setting> StoreSettingsQuery(string storeId)
    {
        return _settingRepository.Table.Where(x => x.StoreId == storeId || x.StoreId == "" || x.StoreId == null);
    }

    private static ISettings CreateSettings(StoreSettings storeSettings, Type type)
    {
        return storeSettings.Find(type.Name.ToLowerInvariant(), type) as ISettings ??
               Activator.CreateInstance(type) as ISettings;
    }

    /// <summary>
    ///     The setting rows of one store and the global ones, with the values already read from them
    /// </summary>
    private sealed class StoreSettings
    {
        private readonly ILookup<string, Setting> _rows;
        private readonly ConcurrentDictionary<(string name, Type type), object> _values = new();

        public StoreSettings(IEnumerable<Setting> rows, string storeId)
        {
            _rows = rows.ToLookup(x => x.Name);
            StoreId = storeId ?? "";
        }

        public string StoreId { get; }

        /// <summary>
        ///     Gets the value of a setting - the row of the store wins over the global one - or null when there is neither
        /// </summary>
        public object Find(string name, Type type)
        {
            return _values.GetOrAdd((name, type), _ =>
            {
                var rows = _rows[name];
                var setting = rows.FirstOrDefault(x => x.StoreId == StoreId) ??
                              rows.FirstOrDefault(x => string.IsNullOrEmpty(x.StoreId));
                return setting != null ? JsonSerializer.Deserialize(setting.Metadata, type) : null;
            });
        }
    }

    #endregion

    #region Fields

    private readonly IRepository<Setting> _settingRepository;
    private readonly ICacheBase _cacheBase;

    //the settings read without the cache, kept for the rest of this scope
    private StoreSettings _uncachedStoreSettings;

    #endregion

    #region Methods

    /// <summary>
    ///     Adds a setting
    /// </summary>
    /// <param name="setting">Setting</param>
    /// <param name="clearCache">A value indicating whether to clear cache after setting update</param>
    public virtual async Task InsertSetting(Setting setting)
    {
        ArgumentNullException.ThrowIfNull(setting);

        await _settingRepository.InsertAsync(setting);

        //cache
        _uncachedStoreSettings = null;
        await _cacheBase.Clear();
    }

    /// <summary>
    ///     Updates a setting
    /// </summary>
    /// <param name="setting">Setting</param>
    /// <param name="clearCache">A value indicating whether to clear cache after setting update</param>
    public virtual async Task UpdateSetting(Setting setting)
    {
        ArgumentNullException.ThrowIfNull(setting);

        await _settingRepository.UpdateAsync(setting);

        //cache
        _uncachedStoreSettings = null;
        await _cacheBase.Clear();
    }

    /// <summary>
    ///     Deletes a setting
    /// </summary>
    /// <param name="setting">Setting</param>
    public virtual async Task DeleteSetting(Setting setting)
    {
        ArgumentNullException.ThrowIfNull(setting);

        await _settingRepository.DeleteAsync(setting);

        //cache
        _uncachedStoreSettings = null;
        await _cacheBase.Clear();
    }

    /// <summary>
    ///     Gets a setting by identifier
    /// </summary>
    /// <param name="settingId">Setting identifier</param>
    /// <returns>Setting</returns>
    public virtual Task<Setting> GetSettingById(string settingId)
    {
        return _settingRepository.GetByIdAsync(settingId);
    }


    /// <summary>
    ///     Get setting value by key
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    /// <param name="key">Key</param>
    /// <param name="defaultValue">Default value</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>Setting value</returns>
    public virtual async Task<T> GetSettingByKey<T>(string key, T defaultValue = default, string storeId = "")
    {
        if (string.IsNullOrEmpty(key))
            return defaultValue;

        var storeSettings = await GetStoreSettings(storeId);
        return storeSettings.Find(key.ToLowerInvariant(), typeof(T)) is T value ? value : defaultValue;
    }

    /// <summary>
    ///     Set setting value
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    /// <param name="key">Key</param>
    /// <param name="value">Value</param>
    /// <param name="storeId">Store identifier</param>
    /// <param name="clearCache">A value indicating whether to clear cache after setting update</param>
    public virtual async Task SetSetting<T>(string key, T value, string storeId = "")
    {
        ArgumentNullException.ThrowIfNull(key);

        key = key.Trim().ToLowerInvariant();

        var settings = GetSettingsByName(key);
        var setting = settings.Any() ? settings.FirstOrDefault(x => x.StoreId == storeId) : null;
        if (setting != null)
        {
            //update
            setting.Metadata = JsonSerializer.Serialize(value);
            await UpdateSetting(setting);
        }
        else
        {
            //insert
            var metadata = JsonSerializer.Serialize(value);
            setting = new Setting {
                Name = key.ToLowerInvariant(),
                Metadata = metadata,
                StoreId = storeId
            };
            await InsertSetting(setting);
        }
    }

    /// <summary>
    ///     Gets all settings
    /// </summary>
    /// <returns>Settings</returns>
    public virtual IList<Setting> GetAllSettings()
    {
        return _settingRepository.Table.OrderBy(x => x.Name).ToList();
    }

    /// <summary>
    ///     Load settings
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    /// <param name="storeId">Store identifier for which settings should be loaded</param>
    public virtual async Task<T> LoadSetting<T>(string storeId = "") where T : ISettings, new()
    {
        return (T)CreateSettings(await GetStoreSettings(storeId), typeof(T));
    }

    /// <summary>
    ///     Reads the settings of a store, and the global ones, into the cache, so that settings resolved from the container need no query
    /// </summary>
    /// <param name="storeId">Store identifier</param>
    public virtual Task LoadStoreSettings(string storeId)
    {
        return GetStoreSettings(storeId);
    }

    /// <summary>
    ///     Load settings
    /// </summary>
    /// <param name="type">Type</param>
    /// <param name="storeId">Store identifier for which settings should be loaded</param>
    public virtual ISettings LoadSetting(Type type, string storeId = "")
    {
        //called by the container, which cannot wait for a query: LoadStoreSettings reads the store settings ahead
        //when the store context is set. Without them - a scope with no store context, or the cache cleared
        //meanwhile - they are read here once and kept for the other settings this scope resolves
        storeId ??= "";
        if (_cacheBase.TryGet(StoreSettingsKey(storeId), out StoreSettings storeSettings))
            return CreateSettings(storeSettings, type);

        if (_uncachedStoreSettings?.StoreId != storeId)
            _uncachedStoreSettings = new StoreSettings(StoreSettingsQuery(storeId).ToList(), storeId);

        return CreateSettings(_uncachedStoreSettings, type);
    }

    /// <summary>
    ///     Save settings object
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    /// <param name="storeId">Store identifier</param>
    /// <param name="settings">Setting instance</param>
    public virtual async Task SaveSetting<T>(T settings, string storeId = "") where T : ISettings, new()
    {
        var dbSettings = GetSettingsByName(typeof(T).Name);
        var setting = dbSettings.Any() ? dbSettings.FirstOrDefault(x => x.StoreId == storeId) : null;
        if (setting != null)
        {
            //update
            setting.Metadata = JsonSerializer.Serialize(settings);
            await UpdateSetting(setting);
        }
        else
        {
            //insert
            setting = new Setting {
                Name = typeof(T).Name.ToLowerInvariant(),
                Metadata = JsonSerializer.Serialize(settings),
                StoreId = storeId
            };
            await InsertSetting(setting);
        }
    }

    /// <summary>
    ///     Delete all settings
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    public virtual async Task DeleteSetting<T>() where T : ISettings, new()
    {
        var settingsToDelete = GetSettingsByName(typeof(T).Name);

        foreach (var setting in settingsToDelete)
            await DeleteSetting(setting);
    }

    /// <summary>
    ///     Clear cache
    /// </summary>
    public virtual async Task ClearCache()
    {
        _uncachedStoreSettings = null;
        await _cacheBase.RemoveByPrefix(CacheKey.SETTINGS_PATTERN_KEY);
    }

    #endregion
}