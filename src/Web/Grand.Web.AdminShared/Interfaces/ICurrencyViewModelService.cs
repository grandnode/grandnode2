using Grand.Domain.Directory;
using Grand.Web.AdminShared.Models.Directory;

namespace Grand.Web.AdminShared.Interfaces;

public interface ICurrencyViewModelService
{
    CurrencyModel PrepareCurrencyModel();
    Task<Currency> InsertCurrencyModel(CurrencyModel model);
    Task<Currency> UpdateCurrencyModel(Currency currency, CurrencyModel model);
    Task MarkAsPrimaryExchangeRateCurrency(string id);
    Task MarkAsPrimaryStoreCurrency(string id);
    Task<(bool canProceed, string message)> ValidateCurrencyUnpublish(string currencyId, bool published);
    Task<(bool canDelete, string message)> ValidateCurrencyDelete(Currency currency);
}