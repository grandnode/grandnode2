namespace Grand.Domain.Data
{
    /// <summary>
    /// Data settings (connection string information)
    /// </summary>
    public partial class DataSettings
    {

        /// <summary>
        /// Connection string
        /// </summary>
        public string DataConnectionString { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(DataConnectionString);
        }
    }
}
