using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Directory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallCurrencies()
        {
            var currencies = new List<Currency>
            {
                new Currency
                {
                    Name = "US Dollar",
                    CurrencyCode = "USD",
                    Rate = 1,
                    DisplayLocale = "en-US",
                    CustomFormatting = "",
                    NumberDecimal = 2,
                    Published = true,
                    DisplayOrder = 1,
                    RoundingTypeId = RoundingType.Rounding001,
                    MidpointRoundId = MidpointRounding.ToEven,
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow,
                },
                new Currency
                {
                    Name = "Euro",
                    CurrencyCode = "EUR",
                    Rate = 0.95,
                    DisplayLocale = "",
                    CustomFormatting = string.Format("{0}0.00", "\u20ac"),
                    NumberDecimal = 2,
                    Published = true,
                    DisplayOrder = 2,
                    RoundingTypeId = RoundingType.Rounding001,
                    MidpointRoundId = MidpointRounding.AwayFromZero,
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow,
                },
                new Currency
                {
                    Name = "British Pound",
                    CurrencyCode = "GBP",
                    Rate = 0.82,
                    DisplayLocale = "en-GB",
                    CustomFormatting = "",
                    NumberDecimal = 2,
                    Published = false,
                    DisplayOrder = 3,
                    RoundingTypeId = RoundingType.Rounding001,
                    MidpointRoundId = MidpointRounding.AwayFromZero,
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow,
                },
                new Currency
                {
                    Name = "Chinese Yuan Renminbi",
                    CurrencyCode = "CNY",
                    Rate = 6.93,
                    DisplayLocale = "zh-CN",
                    CustomFormatting = "",
                    NumberDecimal = 2,
                    Published = false,
                    DisplayOrder = 4,
                    RoundingTypeId = RoundingType.Rounding001,
                    MidpointRoundId = MidpointRounding.ToEven,
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow,
                },
                new Currency
                {
                    Name = "Indian Rupee",
                    CurrencyCode = "INR",
                    Rate = 68.17,
                    DisplayLocale = "en-IN",
                    CustomFormatting = "",
                    NumberDecimal = 2,
                    Published = false,
                    DisplayOrder = 5,
                    RoundingTypeId = RoundingType.Rounding001,
                    MidpointRoundId = MidpointRounding.ToEven,
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow,
                },
                new Currency
                {
                    Name = "Złoty",
                    CurrencyCode = "PLN",
                    Rate = 3.97,
                    DisplayLocale = "pl-PL",
                    CustomFormatting = "",
                    NumberDecimal = 2,
                    Published = false,
                    DisplayOrder = 6,
                    RoundingTypeId = RoundingType.Rounding001,
                    MidpointRoundId = MidpointRounding.AwayFromZero,
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow,
                },
            };
            await _currencyRepository.InsertAsync(currencies);
        }
    }
}
