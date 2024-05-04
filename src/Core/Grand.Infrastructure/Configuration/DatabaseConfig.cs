namespace Grand.Infrastructure.Configuration;

public class DatabaseConfig
{
    /// <summary>
    ///     Gets or sets a value indicating whether use LiteDB database for your application
    /// </summary>
    public bool UseLiteDb { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether use LiteDB database as a singleton service in the application
    /// </summary>
    public bool Singleton { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether use LiteDB database (only for installation process)
    /// </summary>
    public string LiteDbConnectionString { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether use connection string for database (only for installed databases)
    /// </summary>
    public string ConnectionString { get; set; }

    public int DbProvider { get; set; }
}