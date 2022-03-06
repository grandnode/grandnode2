namespace Grand.Infrastructure.Configuration
{
    public partial class LiteDbConfig
    {
        /// <summary>
        /// Gets or sets a value indicating whether use LiteDB database for your application
        /// </summary>
        public bool UseLiteDb { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether use LiteDB database as a singleton service in the application
        /// </summary>
        public bool Singleton{ get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether use LiteDB database (only for installation process)
        /// </summary>
        public string LiteDbConnectionString { get; set; }
    }
}
