using Grand.Business.Catalog.Services.Prices;
using Grand.Business.Common.Services.Directory;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Data;
using Grand.Data.Mongo;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Globalization;

namespace Grand.Business.Catalog.Tests.Services.Prices;

[TestClass]
public class PriceFormatterTests
{
    private IAclService _aclService;
    private IRepository<Currency> _currencyRepo;
    private ICurrencyService _currencyService;
    private CurrencySettings _currencySettings;
    private IMediator _eventPublisher;
    private IPriceFormatter _priceFormatter;
    private IServiceProvider _serviceProvider;

    private TaxSettings _taxSettings;
    private ITranslationService _translationService;
    private IWorkContext _workContext;
    private Mock<IWorkContext> tempWorkContext;

    [TestInitialize]
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
            Published = true
        };

        var currency02 = new Currency {
            Id = "1",
            Name = "US Dollar",
            CurrencyCode = "USD",
            DisplayLocale = "en-US",
            CustomFormatting = "",
            DisplayOrder = 2,
            Published = true
        };

        var tempCurrencyRepo = new Mock<IRepository<Currency>>();
        {
            var tempIMongoCollection = new Mock<MongoRepository<Currency>>(Mock.Of<IAuditInfoProvider>()).Object;
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
            tempLocalizationService.Setup(x => x.GetResource("Products.InclTaxSuffix", "1", "", false))
                .Returns("{0} incl tax");
            tempLocalizationService.Setup(x => x.GetResource("Products.ExclTaxSuffix", "1", "", false))
                .Returns("{0} excl tax");
            _translationService = tempLocalizationService.Object;
        }

        _priceFormatter = new PriceFormatter(_workContext);
    }

    [TestMethod]
    public void Can_formatPrice_with_custom_currencyFormatting()
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

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

        Assert.AreEqual("€412.20", _priceFormatter.FormatPrice(412.2, currency0111));
    }

    [TestMethod]
    public void Can_formatPrice_with_distinct_currencyDisplayLocale()
    {
        var usd_currency = new Currency {
            Id = "1",
            Name = "US Dollar",
            CurrencyCode = "USD",
            DisplayLocale = "en-US",
            MidpointRoundId = MidpointRounding.AwayFromZero
        };
        var gbp_currency = new Currency {
            Id = "2",
            Name = "great british pound",
            CurrencyCode = "GBP",
            DisplayLocale = "en-GB",
            MidpointRoundId = MidpointRounding.AwayFromZero
        };
        var euro_currency = new Currency {
            Id = "3",
            Name = "Euro",
            CurrencyCode = "EUR",
            DisplayLocale = "en_150",
            MidpointRoundId = MidpointRounding.AwayFromZero
        };
        var language = new Language {
            Id = "1",
            Name = "English",
            LanguageCulture = "en-US"
        };
        Assert.AreEqual("$1,234.50", _priceFormatter.FormatPrice(1234.5, usd_currency));
        Assert.AreEqual("£1,234.50", _priceFormatter.FormatPrice(1234.5, gbp_currency));
    }

    [TestMethod]
    public void Can_formatPrice_with_showCurrencyCode()
    {
        //DisplayCurrecyLabel = true            $123.00 (USD)
        //DisplayCurrecyLabel = false           $123.00

        var currency = new Currency {
            Id = "1",
            Name = "US Dollar",
            CurrencyCode = "USD",
            DisplayLocale = "en-US"
        };
        var language = new Language {
            Id = "1",
            Name = "English",
            LanguageCulture = "en-US"
        };

        Assert.AreEqual("$18,888.10", _priceFormatter.FormatPrice(18888.1, currency));
    }
}