using Grand.Domain.Directory;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Common.Interfaces.Directory
{
    /// <summary>
    /// Country service interface
    /// </summary>
    public partial interface ICountryService
    {      
        /// <summary>
        /// Gets all countries
        /// </summary>
        /// <param name="languageId">Language identifier</param>
        /// <param name="storeId">Store ident</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Countries</returns>
        Task<IList<Country>> GetAllCountries(string languageId = "", string storeId = "", bool showHidden = false);
        /// <summary>
        /// Gets all countries that allow billing
        /// </summary>
        /// <param name="languageId">Language identifier.</param>
        /// <param name="storeId">Store ident</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Countries</returns>
        Task<IList<Country>> GetAllCountriesForBilling(string languageId = "", string storeId = "", bool showHidden = false);

        /// <summary>
        /// Gets all countries that allow shipping
        /// </summary>
        /// <param name="languageId">Language identifier.</param>
        /// <param name="storeId">Store ident</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Countries</returns>
        Task<IList<Country>> GetAllCountriesForShipping(string languageId = "", string storeId = "", bool showHidden = false);

        /// <summary>
        /// Gets a country 
        /// </summary>
        /// <param name="countryId">Country identifier</param>
        /// <returns>Country</returns>
        Task<Country> GetCountryById(string countryId);

        /// <summary>
        /// Get countries by identifiers
        /// </summary>
        /// <param name="countryIds">Country identifiers</param>
        /// <returns>Countries</returns>
        Task<IList<Country>> GetCountriesByIds(string[] countryIds);

        /// <summary>
        /// Gets a country by two letter ISO code
        /// </summary>
        /// <param name="twoLetterIsoCode">Country two letter ISO code</param>
        /// <returns>Country</returns>
        Task<Country> GetCountryByTwoLetterIsoCode(string twoLetterIsoCode);

        /// <summary>
        /// Gets a country by three letter ISO code
        /// </summary>
        /// <param name="threeLetterIsoCode">Country three letter ISO code</param>
        /// <returns>Country</returns>
        Task<Country> GetCountryByThreeLetterIsoCode(string threeLetterIsoCode);

        /// <summary>
        /// Inserts a country
        /// </summary>
        /// <param name="country">Country</param>
        Task InsertCountry(Country country);

        /// <summary>
        /// Updates the country
        /// </summary>
        /// <param name="country">Country</param>
        Task UpdateCountry(Country country);

        /// <summary>
        /// Deletes a country
        /// </summary>
        /// <param name="country">Country</param>
        Task DeleteCountry(Country country);

        #region State province

        /// <summary>
        /// Gets a state/province collection by country identifier
        /// </summary>
        /// <param name="countryId">Country identifier</param>
        /// <param name="languageId">Language identifier.</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>States</returns>
        Task<IList<StateProvince>> GetStateProvincesByCountryId(string countryId, string languageId = "", bool showHidden = false);

        /// <summary>
        /// Inserts a state/province
        /// </summary>
        /// <param name="stateProvince">State/province</param>
        /// <param name="countryId">Country ident</param>
        Task InsertStateProvince(StateProvince stateProvince, string countryId);

        /// <summary>
        /// Updates a state/province
        /// </summary>
        /// <param name="stateProvince">State/province</param>
        /// <param name="countryId">Country ident</param>
        Task UpdateStateProvince(StateProvince stateProvince, string countryId);

        /// <summary>
        /// Deletes a state/province
        /// </summary>
        /// <param name="stateProvince">The state/province</param>
        /// <param name="countryId">Country ident</param>
        Task DeleteStateProvince(StateProvince stateProvince, string countryId);


        #endregion


    }
}