using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Services.Prices;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Directory;
using Grand.Domain.Data;
using Grand.Domain.Data.Mongo;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;
using System.Threading;

namespace Grand.Business.Catalog.Tests.Service.Prices
{
    [TestClass()]
    public class PriceFormatterTests
    {
        private IRepository<Currency> _currencyRepo;
        private ICurrencyService _currencyService;
        private CurrencySettings _currencySettings;
        private IWorkContext _workContext;
        private Mock<IWorkContext> tempWorkContext;

        private TaxSettings _taxSettings;
        private IPriceFormatter _priceFormatter;
        private IAclService _aclService;
        private IServiceProvider _serviceProvider;
        private IMediator _eventPublisher;
        private ITranslationService _translationService;

        [TestInitialize()]
        public void TestInitialize()
        {
            CommonPath.BaseDirectory = "";

            var eventPublisher = new Mock<IMediator>();
            _eventPublisher = eventPublisher.Object;

            var cacheManager = new Mock<ICacheBase>();
            tempWorkContext = new Mock<IWorkContext>();
            {
                _workContext = tempWorkContext.Object;
            }
            _currencySettings = new CurrencySettings();
            var currency01 = new Currency {
                Id = "1",
                Name = "Euro",
                CurrencyCode = "EUR",
                DisplayLocale = "",
                CustomFormatting = "€0.00",
                DisplayOrder = 1,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };

            var currency02 = new Currency {
                Id = "1",
                Name = "US Dollar",
                CurrencyCode = "USD",
                DisplayLocale = "en-US",
                CustomFormatting = "",
                DisplayOrder = 2,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };

            var tempCurrencyRepo = new Mock<IRepository<Currency>>();
            {
                var tempIMongoCollection = new Mock<MongoRepository<Currency>>().Object;
                tempIMongoCollection.Insert(currency01);
                tempIMongoCollection.Insert(currency02);
                tempCurrencyRepo.Setup(x => x.Table).Returns(tempIMongoCollection.Table);
            }

            _aclService = new Mock<IAclService>().Object;
            _serviceProvider = new Mock<IServiceProvider>().Object;

            _currencyRepo = new Mock<IRepository<Currency>>().Object;


            _currencyService = new CurrencyService(
                cacheManager.Object,
                _currencyRepo,
                _aclService,
                _currencySettings,
                null);

            _taxSettings = new TaxSettings();

            var tempLocalizationService = new Mock<ITranslationService>();
            {
                tempLocalizationService.Setup(x => x.GetResource("Products.InclTaxSuffix", "1", "", false)).Returns("{0} incl tax");
                tempLocalizationService.Setup(x => x.GetResource("Products.ExclTaxSuffix", "1", "", false)).Returns("{0} excl tax");
                _translationService = tempLocalizationService.Object;
            }

            _priceFormatter = new PriceFormatter(_workContext, _currencyService, _translationService, _taxSettings);
        }

        [TestMethod()]
        public void Can_formatPrice_with_custom_currencyFormatting()
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            var currency0111 = new Currency {
                Id = "1",
                Name = "Euro",
                CurrencyCode = "EUR",
                DisplayLocale = "",
                CustomFormatting = "€0.00",
                DisplayOrder = 1,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                RoundingTypeId = RoundingType.Rounding001,
                MidpointRoundId = MidpointRounding.AwayFromZero
            };

            var language0111 = new Language {
                Id = "1",
                Name = "English",
                LanguageCulture = "en-US"
            };

            Assert.AreEqual("€412.20", _priceFormatter.FormatPrice(412.2, currency0111, language0111, false, false));
        }

        [TestMethod()]
        public void Can_formatPrice_with_distinct_currencyDisplayLocale()
        {

            var usd_currency = new Currency {
                Id = "1",
                Name = "US Dollar",
                CurrencyCode = "USD",
                DisplayLocale = "en-US",
                MidpointRoundId = MidpointRounding.AwayFromZero,

            };
            var gbp_currency = new Currency {
                Id = "2",
                Name = "great british pound",
                CurrencyCode = "GBP",
                DisplayLocale = "en-GB",
                MidpointRoundId = MidpointRounding.AwayFromZero,
            };
            var euro_currency = new Currency {
                Id = "3",
                Name = "Euro",
                CurrencyCode = "EUR",
                DisplayLocale = "en_150",
                MidpointRoundId = MidpointRounding.AwayFromZero,
            };
            var language = new Language {
                Id = "1",
                Name = "English",
                LanguageCulture = "en-US"
            };
            Assert.AreEqual("$1,234.50", _priceFormatter.FormatPrice(1234.5, usd_currency, language, false, false));
            Assert.AreEqual("£1,234.50", _priceFormatter.FormatPrice(1234.5, gbp_currency, language, false, false));
        }

        [TestMethod()]
        public void Can_formatPrice_with_showTax()
        {
            //$18,888.10                    priceIncludestax=false || showTax=false
            //$18,888.10 incl tax           priceIncludestax=true || showTax=true
            //$18,888.10 excl tax           priceIncludestax=false || showTax=true

            var currency = new Currency {
                Id = "1",
                Name = "US Dollar",
                CurrencyCode = "USD",
                DisplayLocale = "en-US",
                MidpointRoundId = MidpointRounding.AwayFromZero,
            };
            var language = new Language {
                Id = "1",
                Name = "English",
                LanguageCulture = "en-US"
            };

            //
            Assert.AreEqual("$18,888.10", _priceFormatter.FormatPrice(18888.1, currency, language, false, false));
            //
            Assert.AreEqual("$18,888.10 incl tax", _priceFormatter.FormatPrice(18888.1, currency, language, true, true));
            //
            Assert.AreEqual("$18,888.10 excl tax", _priceFormatter.FormatPrice(18888.1, currency, language, false, true));
        }

        [TestMethod()]
        public void Can_formatPrice_with_showCurrencyCode()
        {
            //DisplayCurrecyLabel = true            $123.00 (USD)
            //DisplayCurrecyLabel = false           $123.00

            var currency = new Currency {
                Id = "1",
                Name = "US Dollar",
                CurrencyCode = "USD",
                DisplayLocale = "en-US",
            };
            var language = new Language {
                Id = "1",
                Name = "English",
                LanguageCulture = "en-US"
            };

            Assert.AreEqual("$18,888.10", _priceFormatter.FormatPrice(18888.1, currency, language, false, false));

        }
    }
}
