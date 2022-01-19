using Grand.Domain.Directory;
using Grand.Web.Admin.Models.Directory;

namespace Grand.Web.Admin.Interfaces
{
    public interface ICurrencyViewModelService
    {
        CurrencyModel PrepareCurrencyModel();
        Task<Currency> InsertCurrencyModel(CurrencyModel model);
        Task<Currency> UpdateCurrencyModel(Currency currency, CurrencyModel model);
    }
}
