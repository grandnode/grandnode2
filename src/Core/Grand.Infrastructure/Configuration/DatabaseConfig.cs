namespace Grand.Infrastructure.Configuration;

public class DatabaseConfig
{
    /// <summary>
    ///     Gets or sets a value indicating whether use LiteDB database for your application
    /// </summary>
    public bool UseLiteDb { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether use LiteDB database (only for installation process)
    /// </summary>
    public string LiteDbConnectionString { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether storefront product keyword search uses a text index (whole
    ///     words only; MongoDB and Azure DocumentDB, ignored with LiteDB)
    /// </summary>
    public bool UseProductTextSearch { get; set; }
}