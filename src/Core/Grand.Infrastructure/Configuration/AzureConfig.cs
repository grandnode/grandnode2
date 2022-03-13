namespace Grand.Infrastructure.Configuration
{
    public partial class AzureConfig
    {
        /// <summary>
        /// A value indicating whether the site is run on Windows Azure Web Apps
        /// </summary>
        public bool RunOnAzureWebApps { get; set; }

        /// <summary>
        /// Connection string for Azure BLOB storage
        /// </summary>
        public string AzureBlobStorageConnectionString { get; set; }

        /// <summary>
        /// Container name for Azure BLOB storage
        /// </summary>
        public string AzureBlobStorageContainerName { get; set; }
        /// <summary>
        /// End point for Azure BLOB storage
        /// </summary>
        public string AzureBlobStorageEndPoint { get; set; }

    }
}
