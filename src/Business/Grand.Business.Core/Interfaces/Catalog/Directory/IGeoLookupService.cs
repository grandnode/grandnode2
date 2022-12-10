namespace Grand.Business.Core.Interfaces.Catalog.Directory
{
    /// <summary>
    /// GEO lookup service
    /// </summary>
    public interface IGeoLookupService
    {
        /// <summary>
        /// Get country name
        /// </summary>
        /// <param name="ipAddress">IP address</param>
        /// <returns>Country name</returns>
        string CountryIsoCode(string ipAddress);

        /// <summary>
        /// Get country name
        /// </summary>
        /// <param name="ipAddress">IP address</param>
        /// <returns>Country name</returns>
        string CountryName(string ipAddress);
    }
}