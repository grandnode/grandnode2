using Grand.Domain;
using Tax.CountryStateZip.Domain;

namespace Tax.CountryStateZip.Services;

/// <summary>
///     Tax rate service interface
/// </summary>
public interface ITaxRateService
{
    /// <summary>
    ///     Deletes a tax rate
    /// </summary>
    /// <param name="taxRate">Tax rate</param>
    Task DeleteTaxRate(TaxRate taxRate);

    /// <summary>
    ///     Gets all tax rates
    /// </summary>
    /// <param name="storeId">Store identifier; pass an empty string to return rates of all stores</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Tax rates</returns>
    Task<IPagedList<TaxRate>> GetAllTaxRates(string storeId = "", int pageIndex = 0, int pageSize = int.MaxValue);

    /// <summary>
    ///     Gets a tax rate
    /// </summary>
    /// <param name="taxRateId">Tax rate identifier</param>
    /// <returns>Tax rate</returns>
    Task<TaxRate> GetTaxRateById(string taxRateId);

    /// <summary>
    ///     Inserts a tax rate
    /// </summary>
    /// <param name="taxRate">Tax rate</param>
    Task InsertTaxRate(TaxRate taxRate);

    /// <summary>
    ///     Updates the tax rate
    /// </summary>
    /// <param name="taxRate">Tax rate</param>
    Task UpdateTaxRate(TaxRate taxRate);
}