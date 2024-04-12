//This product contains GeoLite2 data created by MaxMind, from http://www.maxmind.com

using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.SharedKernel.Extensions;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;
using MaxMind.GeoIP2.Responses;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Catalog.Services.Directory;

/// <summary>
///     GEO lookup service
/// </summary>
public class GeoLookupService : IGeoLookupService
{
    #region Fields

    private readonly ILogger<GeoLookupService> _logger;

    #endregion

    #region Ctor

    public GeoLookupService(ILogger<GeoLookupService> logger)
    {
        _logger = logger;
    }

    #endregion

    #region Utilities

    protected virtual CountryResponse GetInformation(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return null;

        try
        {
            //This product includes GeoLite2 data created by MaxMind, available from http://www.maxmind.com
            var databasePath = CommonPath.MapPath("App_Data/GeoLite2-Country.mmdb");
            var reader = new DatabaseReader(databasePath);
            var response = reader.Country(ipAddress);
            return response;
            //more info: http://maxmind.github.io/GeoIP2-dotnet/
            //more info: https://github.com/maxmind/GeoIP2-dotnet
        }
        catch (GeoIP2Exception)
        {
            return null;
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "Cannot load MaxMind record");
            return null;
        }
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Get country name
    /// </summary>
    /// <param name="ipAddress">IP address</param>
    /// <returns>Country name</returns>
    public virtual string CountryIsoCode(string ipAddress)
    {
        var response = GetInformation(ipAddress);
        return response is not null ? response.Country.IsoCode : "";
    }

    /// <summary>
    ///     Get country name
    /// </summary>
    /// <param name="ipAddress">IP address</param>
    /// <returns>Country name</returns>
    public virtual string CountryName(string ipAddress)
    {
        var response = GetInformation(ipAddress);
        return response is not null ? response.Country.Name : "";
    }

    #endregion
}