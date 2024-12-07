using System.Text.Json;

namespace Grand.Data;

/// <summary>
///     Manager of data settings (connection string)
/// </summary>
public sealed class DataSettingsManager
{
    private static DataSettingsManager _instance;
    private static readonly object _lock = new();

    public static DataSettingsManager Instance => _instance ?? throw new InvalidOperationException("DataSettingsManager has not been initialized. Call Initialize first.");

    private readonly string _settingsPath;

    private DataSettings _dataSettings;

    private bool? _databaseIsInstalled;

    
    public static void Initialize(string settingsPath)
    {
        if (_instance == null)
        {
            lock (_lock)
            {
                _instance ??= new DataSettingsManager(settingsPath);
            }
        }
    }
    private DataSettingsManager(string settingsPath)
    {
        _settingsPath = settingsPath;
    }

    /// <summary>
    ///     Load settings
    /// </summary>
    public DataSettings LoadSettings(bool reloadSettings = false)
    {
        if (!reloadSettings && _dataSettings != null)
            return _dataSettings;

        if (!File.Exists(_settingsPath))
            return new DataSettings();

        try
        {
            var text = File.ReadAllText(_settingsPath);
            _dataSettings = JsonSerializer.Deserialize<DataSettings>(text);
        }
        catch
        {
            //Try to read file
            var connectionString = File.ReadLines(_settingsPath).FirstOrDefault();
            _dataSettings = new DataSettings { ConnectionString = connectionString, DbProvider = DbProvider.MongoDB };
        }

        return _dataSettings;
    }

    public DataSettings LoadDataSettings(DataSettings dataSettings)
    {
        _dataSettings = dataSettings;
        return _dataSettings;
    }


    /// <summary>
    ///     Returns a value indicating whether database is already installed
    /// </summary>
    /// <returns></returns>
    public static bool DatabaseIsInstalled()
    {
        if (_instance == null)
            throw new InvalidOperationException("DataSettingsManager has not been initialized. Call Initialize first.");

        if (!_instance._databaseIsInstalled.HasValue)
        {
            var settings = _instance._dataSettings ?? _instance.LoadSettings();
            _instance._databaseIsInstalled = settings != null && !string.IsNullOrEmpty(settings.ConnectionString);
        }

        return _instance._databaseIsInstalled.Value;
    }

    public void ResetCache()
    {
        _databaseIsInstalled = false;
    }

    /// <summary>
    ///     Save settings to a file
    /// </summary>
    /// <param name="settings"></param>
    public async Task SaveSettings(DataSettings settings)
    {
        if (!File.Exists(_settingsPath))
        {
            await using var fs = File.Create(_settingsPath);
        }

        var data = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_settingsPath, data);
    }
}