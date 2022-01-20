namespace Grand.Infrastructure.Migrations
{
    public interface IBaseMigration
    {
        /// <summary>
        /// Field to which version should be upgraded db
        /// </summary>
        DbVersion Version { get; }

        /// <summary>
        /// The unique identity of migration.
        /// </summary>
        Guid Identity { get; }

        /// <summary>
        /// Name of migration.
        /// </summary>
        string Name { get; }
    }
}
