namespace Grand.Domain;

public class MigrationDb : BaseEntity
{
    /// <summary>
    ///     The unique identity of migration.
    /// </summary>
    public Guid Identity { get; set; }

    /// <summary>
    ///     Name of migration.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Db Version
    /// </summary>
    public string Version { get; set; }
}