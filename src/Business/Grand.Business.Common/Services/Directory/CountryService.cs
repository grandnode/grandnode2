using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Data;
using Grand.Domain.Directory;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Common.Services.Directory;

/// <summary>
///     Country service
/// </summary>
public class CountryService : ICountryService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="countryRepository">Country repository</param>
    /// <param name="mediator">Mediator</param>
    /// <param name="cacheBase">Cache manager</param>
    /// <param name="accessControlConfig">Access control</param>
    public CountryService(
        IRepository<Country> countryRepository,
        IMediator mediator,
        ICacheBase cacheBase,
        AccessControlConfig accessControlConfig)
    {
        _cacheBase = cacheBase;
        _accessControlConfig = accessControlConfig;
        _countryRepository = countryRepository;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private readonly IRepository<Country> _countryRepository;
    private readonly IMediator _mediator;
    private readonly ICacheBase _cacheBase;
    private readonly AccessControlConfig _accessControlConfig;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets all countries
    /// </summary>
    /// <param name="languageId">Language identifier.</param>
    /// <param name="storeId">Store ident</param>
    /// <param name="showHidden">A value indicating whether to show hidden records</param>
    /// <returns>Countries</returns>
    public virtual async Task<IList<Country>> GetAllCountries(string languageId = "", string storeId = "",
        bool showHidden = false)
    {
        var key = string.Format(CacheKey.COUNTRIES_ALL_KEY, languageId, storeId, showHidden);

        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _countryRepository.Table
                select p;

            if (!showHidden)
                query = query.Where(c => c.Published);

            if (!showHidden && !_accessControlConfig.IgnoreStoreLimitations && !string.IsNullOrEmpty(storeId))
                //Store acl
                query = from p in query
                    where !p.LimitedToStores || p.Stores.Contains(storeId)
                    select p;

            var countries = await Task.FromResult(query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToList());
            if (!string.IsNullOrEmpty(languageId))
                countries = countries
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.GetTranslation(x => x.Name, languageId))
                    .ToList();
            return countries;
        });
    }

    /// <summary>
    ///     Gets all countries that allow billing
    /// </summary>
    /// <param name="languageId">Language identifier.</param>
    /// <param name="storeId">Store ident</param>
    /// <param name="showHidden">A value indicating whether to show hidden records</param>
    /// <returns>Countries</returns>
    public virtual async Task<IList<Country>> GetAllCountriesForBilling(string languageId = "", string storeId = "",
        bool showHidden = false)
    {
        var countries = await GetAllCountries(languageId, storeId, showHidden);
        return countries.Where(c => c.AllowsBilling).ToList();
    }

    /// <summary>
    ///     Gets all countries that allow shipping
    /// </summary>
    /// <param name="languageId">Language identifier.</param>
    /// <param name="storeId">Store ident</param>
    /// <param name="showHidden">A value indicating whether to show hidden records</param>
    /// <returns>Countries</returns>
    public virtual async Task<IList<Country>> GetAllCountriesForShipping(string languageId = "", string storeId = "",
        bool showHidden = false)
    {
        var countries = await GetAllCountries(languageId, storeId, showHidden);
        return countries.Where(c => c.AllowsShipping).ToList();
    }

    /// <summary>
    ///     Gets a country
    /// </summary>
    /// <param name="countryId">Country identifier</param>
    /// <returns>Country</returns>
    public virtual async Task<Country> GetCountryById(string countryId)
    {
        if (string.IsNullOrEmpty(countryId))
            return null;

        var key = string.Format(CacheKey.COUNTRIES_BY_KEY, countryId);
        return await _cacheBase.GetAsync(key, () => _countryRepository.GetByIdAsync(countryId));
    }

    /// <summary>
    ///     Get countries by identifiers
    /// </summary>
    /// <param name="countryIds">Country identifiers</param>
    /// <returns>Countries</returns>
    public virtual async Task<IList<Country>> GetCountriesByIds(string[] countryIds)
    {
        if (countryIds == null || countryIds.Length == 0)
            return new List<Country>();

        var query = from c in _countryRepository.Table
            where countryIds.Contains(c.Id)
            select c;
        var countries = await Task.FromResult(query.ToList());
        //sort by passed identifiers
        return countryIds.Select(id => countries.Find(x => x.Id == id)).Where(country => country != null).ToList();
    }

    /// <summary>
    ///     Gets a country by two letter ISO code
    /// </summary>
    /// <param name="twoLetterIsoCode">Country two letter ISO code</param>
    /// <returns>Country</returns>
    public virtual async Task<Country> GetCountryByTwoLetterIsoCode(string twoLetterIsoCode)
    {
        var key = string.Format(CacheKey.COUNTRIES_BY_TWOLETTER, twoLetterIsoCode);
        return await _cacheBase.GetAsync(key, async () =>
        {
            return await _countryRepository.GetOneAsync(x => x.TwoLetterIsoCode == twoLetterIsoCode);
        });
    }

    /// <summary>
    ///     Gets a country by three letter ISO code
    /// </summary>
    /// <param name="threeLetterIsoCode">Country three letter ISO code</param>
    /// <returns>Country</returns>
    public virtual async Task<Country> GetCountryByThreeLetterIsoCode(string threeLetterIsoCode)
    {
        var key = string.Format(CacheKey.COUNTRIES_BY_THREELETTER, threeLetterIsoCode);
        return await _cacheBase.GetAsync(key, async () =>
        {
            return await _countryRepository.GetOneAsync(x => x.ThreeLetterIsoCode == threeLetterIsoCode);
        });
    }

    /// <summary>
    ///     Inserts a country
    /// </summary>
    /// <param name="country">Country</param>
    public virtual async Task InsertCountry(Country country)
    {
        ArgumentNullException.ThrowIfNull(country);

        await _countryRepository.InsertAsync(country);

        await _cacheBase.RemoveByPrefix(CacheKey.COUNTRIES_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(country);
    }

    /// <summary>
    ///     Updates the country
    /// </summary>
    /// <param name="country">Country</param>
    public virtual async Task UpdateCountry(Country country)
    {
        ArgumentNullException.ThrowIfNull(country);

        await _countryRepository.UpdateAsync(country);

        await _cacheBase.RemoveByPrefix(CacheKey.COUNTRIES_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(country);
    }

    /// <summary>
    ///     Deletes a country
    /// </summary>
    /// <param name="country">Country</param>
    public virtual async Task DeleteCountry(Country country)
    {
        ArgumentNullException.ThrowIfNull(country);

        await _countryRepository.DeleteAsync(country);

        await _cacheBase.RemoveByPrefix(CacheKey.COUNTRIES_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(country);
    }


    #region State provinces

    /// <summary>
    ///     Gets a state/province collection by country identifier
    /// </summary>
    /// <param name="countryId">Country identifier</param>
    /// <param name="languageId">Language identifier.</param>
    /// <returns>States</returns>
    public virtual async Task<IList<StateProvince>> GetStateProvincesByCountryId(string countryId,
        string languageId = "")
    {
        if (string.IsNullOrEmpty(countryId))
            return new List<StateProvince>();

        var country = await GetCountryById(countryId);

        return country?.StateProvinces.OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.GetTranslation(x => x.Name, languageId))
            .ToList();
    }

    /// <summary>
    ///     Inserts a state/province
    /// </summary>
    /// <param name="stateProvince">State/province</param>
    /// <param name="countryId">Country ident</param>
    public virtual async Task InsertStateProvince(StateProvince stateProvince, string countryId)
    {
        ArgumentNullException.ThrowIfNull(stateProvince);

        var country = await GetCountryById(countryId);
        if (country == null)
            throw new ArgumentNullException(nameof(country));

        country.StateProvinces.Add(stateProvince);

        await UpdateCountry(country);
    }

    /// <summary>
    ///     Updates a state/province
    /// </summary>
    /// <param name="stateProvince">State/province</param>
    /// <param name="countryId">Country ident</param>
    public virtual async Task UpdateStateProvince(StateProvince stateProvince, string countryId)
    {
        ArgumentNullException.ThrowIfNull(stateProvince);

        var country = await GetCountryById(countryId);
        if (country == null)
            throw new ArgumentNullException(nameof(country));

        if (country.StateProvinces.FirstOrDefault(x => x.Id == stateProvince.Id) != null)
        {
            var state = country.StateProvinces.FirstOrDefault(x => x.Id == stateProvince.Id);
            if (state != null)
            {
                state.Name = stateProvince.Name;
                state.Locales = stateProvince.Locales;
                state.Published = stateProvince.Published;
                state.Abbreviation = stateProvince.Abbreviation;
                state.DisplayOrder = stateProvince.DisplayOrder;
            }
        }

        await UpdateCountry(country);
    }

    /// <summary>
    ///     Deletes a state/province
    /// </summary>
    /// <param name="stateProvince">The state/province</param>
    /// <param name="countryId">Country ident</param>
    public virtual async Task DeleteStateProvince(StateProvince stateProvince, string countryId)
    {
        ArgumentNullException.ThrowIfNull(stateProvince);

        var country = await GetCountryById(countryId);
        if (country == null)
            throw new ArgumentNullException(nameof(country));

        var state = country.StateProvinces.FirstOrDefault(x => x.Id == stateProvince.Id);
        country.StateProvinces.Remove(state);

        await UpdateCountry(country);
    }

    #endregion

    #endregion
}