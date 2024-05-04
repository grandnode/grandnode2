using Grand.Domain.Directory;

namespace Grand.Business.System.Services.Installation;

public partial class InstallationService
{
    protected virtual Task InstallCurrencies()
    {
        var currencies = new List<Currency> {
            new() {
                Name = "US Dollar",
                CurrencyCode = "USD",
                Rate = 1,
                DisplayLocale = "en-US",
                CustomFormatting = "",
                NumberDecimal = 2,
                Published = true,
                DisplayOrder = 1,
                RoundingTypeId = RoundingType.Rounding001,
                MidpointRoundId = MidpointRounding.ToEven
            },
            new() {
                Name = "Euro",
                CurrencyCode = "EUR",
                Rate = 0.95,
                DisplayLocale = "",
                CustomFormatting = $"{"\u20ac"}0.00",
                NumberDecimal = 2,
                Published = true,
                DisplayOrder = 2,
                RoundingTypeId = RoundingType.Rounding001,
                MidpointRoundId = MidpointRounding.AwayFromZero
            },
            new() {
                Name = "British Pound",
                CurrencyCode = "GBP",
                Rate = 0.82,
                DisplayLocale = "en-GB",
                CustomFormatting = "",
                NumberDecimal = 2,
                Published = false,
                DisplayOrder = 3,
                RoundingTypeId = RoundingType.Rounding001,
                MidpointRoundId = MidpointRounding.AwayFromZero
            },
            new() {
                Name = "Chinese Yuan Renminbi",
                CurrencyCode = "CNY",
                Rate = 6.93,
                DisplayLocale = "zh-CN",
                CustomFormatting = "",
                NumberDecimal = 2,
                Published = false,
                DisplayOrder = 4,
                RoundingTypeId = RoundingType.Rounding001,
                MidpointRoundId = MidpointRounding.ToEven
            },
            new() {
                Name = "Indian Rupee",
                CurrencyCode = "INR",
                Rate = 68.17,
                DisplayLocale = "en-IN",
                CustomFormatting = "",
                NumberDecimal = 2,
                Published = false,
                DisplayOrder = 5,
                RoundingTypeId = RoundingType.Rounding001,
                MidpointRoundId = MidpointRounding.ToEven
            },
            new() {
                Name = "Złoty",
                CurrencyCode = "PLN",
                Rate = 3.97,
                DisplayLocale = "pl-PL",
                CustomFormatting = "",
                NumberDecimal = 2,
                Published = false,
                DisplayOrder = 6,
                RoundingTypeId = RoundingType.Rounding001,
                MidpointRoundId = MidpointRounding.AwayFromZero
            }
        };
        currencies.ForEach(x => _currencyRepository.Insert(x));
        return Task.CompletedTask;
    }
}