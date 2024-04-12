namespace Grand.Data;

/// <summary>
///     Data settings (connection string information)
/// </summary>
public class DataSettings
{
    /// <summary>
    ///     Connection string
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    ///     Database type
    /// </summary>
    public DbProvider DbProvider { get; set; }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(ConnectionString);
    }
}