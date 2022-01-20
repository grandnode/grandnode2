namespace Grand.Domain.Data
{
    public class MigrationDb : BaseEntity
    {
        /// <summary>
        /// The unique identity of migration.
        /// </summary>
        public Guid Identity { get; set; }

        /// <summary>
        /// Name of migration.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Db Version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the date and time of migration creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the migration creation from installation
        /// </summary>
        public bool InstallApp { get; set; }
    }
}
