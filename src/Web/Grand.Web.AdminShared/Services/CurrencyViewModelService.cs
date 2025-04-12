using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Directory;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Directory;

namespace Grand.Web.AdminShared.Services;

public class CurrencyViewModelService : ICurrencyViewModelService
{
    #region Fields

    private readonly ICurrencyService _currencyService;

    #endregion

    #region Constructors

    public CurrencyViewModelService(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    #endregion

    public virtual CurrencyModel PrepareCurrencyModel()
    {
        var model = new CurrencyModel {
            //default values
            Published = true,
            Rate = 1
        };
        return model;
    }

    public virtual async Task<Currency> InsertCurrencyModel(CurrencyModel model)
    {
        var currency = model.ToEntity();
        await _currencyService.InsertCurrency(currency);

        return currency;
    }

    public virtual async Task<Currency> UpdateCurrencyModel(Currency currency, CurrencyModel model)
    {
        currency = model.ToEntity(currency);
        await _currencyService.UpdateCurrency(currency);
        return currency;
    }
}