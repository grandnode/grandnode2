using Grand.Domain.Directory;
using Grand.Web.AdminShared.Models.Directory;

namespace Grand.Web.AdminShared.Interfaces;

public interface ICurrencyViewModelService
{
    CurrencyModel PrepareCurrencyModel();
    Task<Currency> InsertCurrencyModel(CurrencyModel model);
    Task<Currency> UpdateCurrencyModel(Currency currency, CurrencyModel model);
}