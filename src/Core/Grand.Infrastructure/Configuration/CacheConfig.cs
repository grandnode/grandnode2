namespace Grand.Infrastructure.Configuration;

public class CacheConfig
{
    /// <summary>
    ///     Gets or sets a value indicating for default cache time in minutes
    /// </summary>
    public int DefaultCacheTimeMinutes { get; set; }

    /// <summary>
    ///     Gets or sets the maximum number of keys held in the memory cache; 0 means no limit
    /// </summary>
    public int MaxEntries { get; set; } = 100000;
}