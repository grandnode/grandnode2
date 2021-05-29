using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Directory;
using Grand.Domain.Data;
using Grand.Domain.Data.Mongo;
using Grand.Domain.Directory;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using Grand.SharedKernel;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Common.Tests.Services.Directory
{
    [TestClass()]
    public class CurrencyServiceTests
    {
        private IRepository<Currency> _currencyRepository;
        private CurrencySettings _currencySettings;
        private IMediator _eventPublisher;
        private ICurrencyService _currencyService;
        private Mock<ICacheBase> _cacheManager;
        private Mock<IAclService> _aclService;
        private IServiceProvider _serviceProvider;

        private Currency currencyUSD, currencyRUR, currencyEUR;
        private Mock<IRepository<Currency>> tempCurrencyRepository;
        private Mock<IMediator> tempEventPublisher;

        [TestInitialize()]
        public void TestInitialize()
        {
            CommonPath.BaseDirectory = "";
            CommonHelper.CacheTimeMinutes = 10;

            currencyUSD = new Currency {
                Id = "1",
                Name = "US Dollar",
                CurrencyCode = "USD",
                Rate = 1.2,
                DisplayLocale = "en-US",
                CustomFormatting = "",
                Published = true,
                DisplayOrder = 1,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
            };
            currencyEUR = new Currency {
                Id = "2",
                Name = "Euro",
                CurrencyCode = "EUR",
                Rate = 1,
                DisplayLocale = "",
                CustomFormatting = "€0.00",
                Published = true,
                DisplayOrder = 2,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
            };
            currencyRUR = new Currency {
                Id = "3",
                Name = "Russian Rouble",
                CurrencyCode = "RUB",
                Rate = 34.5,
                DisplayLocale = "ru-RU",
                CustomFormatting = "",
                Published = true,
                DisplayOrder = 3,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
            };

            tempCurrencyRepository = new Mock<IRepository<Currency>>();
            {
                var IMongoCollection = new Mock<MongoRepository<Currency>>().Object;
                IMongoCollection.Insert(currencyUSD);
                IMongoCollection.Insert(currencyEUR);
                IMongoCollection.Insert(currencyRUR);

                tempCurrencyRepository.Setup(x => x.Table).Returns(IMongoCollection.Table);
                tempCurrencyRepository.Setup(x => x.GetByIdAsync(currencyUSD.Id)).ReturnsAsync(currencyUSD);
                tempCurrencyRepository.Setup(x => x.GetByIdAsync(currencyEUR.Id)).ReturnsAsync(currencyEUR);
                tempCurrencyRepository.Setup(x => x.GetByIdAsync(currencyRUR.Id)).ReturnsAsync(currencyRUR);
                _currencyRepository = tempCurrencyRepository.Object;
            }
            tempEventPublisher = new Mock<IMediator>();
            {
                //tempEventPublisher.Setup(x => x.PublishAsync(It.IsAny<object>()));
                _eventPublisher = tempEventPublisher.Object;
            }

            _cacheManager = new Mock<ICacheBase>();
            _aclService = new Mock<IAclService>();
            _serviceProvider = new Mock<IServiceProvider>().Object;

            _currencySettings = new CurrencySettings();
            _currencySettings.PrimaryStoreCurrencyId = currencyUSD.Id;
            _currencySettings.PrimaryExchangeRateCurrencyId = currencyEUR.Id;


            _currencyService = new CurrencyService(
                _cacheManager.Object, _currencyRepository, _aclService.Object,
                _currencySettings, _eventPublisher);

            //tempDiscountServiceMock.Setup(x => x.GetAllDiscounts(DiscountType.AssignedToCategories, "", "", false)).ReturnsAsync(new List<Discount>());
        }

        [TestMethod()]
        public void Can_convert_currency_1()
        {
            //e.g. 
            //10.1 * 1.5 = 15.15
            Assert.AreEqual(15.15, _currencyService.ConvertCurrency(10.1, 1.5));
            Assert.AreEqual(10.122, _currencyService.ConvertCurrency(10.122, 1));
            Assert.AreEqual(34.4148, _currencyService.ConvertCurrency(10.122, 3.4));
            Assert.AreEqual(0, _currencyService.ConvertCurrency(10.1, 0));
            Assert.AreEqual(0, _currencyService.ConvertCurrency(0, 5));
        }

        [TestMethod()]
        public async Task ConvertFromPrimaryExchangeRateCurrency_ReturnExpectedResult()
        {
            //set primary exchange rate
            _cacheManager.Setup(c => c.GetAsync<Currency>(It.IsAny<string>(), It.IsAny<Func<Task<Currency>>>())).Returns(Task.FromResult(currencyUSD));
            var result = await _currencyService.ConvertFromPrimaryExchangeRateCurrency(100, currencyEUR);
            Assert.AreEqual(100, result);
            currencyEUR.Rate = 2;
            Assert.AreEqual(200, await _currencyService.ConvertFromPrimaryExchangeRateCurrency(100, currencyEUR));
        }

        [TestMethod()]
        public async Task ConvertToPrimaryExchangeRateCurrency_ReturnExpectedResult()
        {
            //set primary exchange rate
            _cacheManager.Setup(c => c.GetAsync<Currency>(It.IsAny<string>(), It.IsAny<Func<Task<Currency>>>())).Returns(Task.FromResult(currencyUSD));
            var result = await _currencyService.ConvertToPrimaryExchangeRateCurrency(100, currencyEUR);
            Assert.AreEqual(100, result);
            currencyEUR.Rate = 2;
            Assert.AreEqual(50, await _currencyService.ConvertToPrimaryExchangeRateCurrency(100, currencyEUR));
        }

        [TestMethod()]
        public void ConvertToPrimaryExchangeRateCurrency_ZeroExchangeRate_ThrowException()
        {
            _cacheManager.Setup(c => c.GetAsync<Currency>(It.IsAny<string>(), It.IsAny<Func<Task<Currency>>>())).Returns(Task.FromResult(currencyUSD));
            currencyEUR.Rate = 0;
            Assert.ThrowsExceptionAsync<GrandException>(async () => await _currencyService.ConvertToPrimaryExchangeRateCurrency(100, currencyEUR));
        }

        [TestMethod()]
        public void ConvertToPrimaryExchangeRateCurrency_CannotLoadPrimaryExchange_ThrowException()
        {
            _cacheManager.Setup(c => c.GetAsync<Currency>(It.IsAny<string>(), It.IsAny<Func<Task<Currency>>>())).Returns(Task.FromResult<Currency>(null));
            Assert.ThrowsExceptionAsync<Exception>(async () => await _currencyService.ConvertToPrimaryExchangeRateCurrency(100, currencyEUR));
        }

        [TestMethod()]
        public async Task GetPrimaryStoreCurrency_ReturnExpectedValue()
        {
            _currencySettings.PrimaryStoreCurrencyId = currencyUSD.Id;
            _cacheManager.Setup(c => c.GetAsync<Currency>(It.IsAny<string>(), It.IsAny<Func<Task<Currency>>>())).Returns(Task.FromResult(currencyUSD));
            var result = await _currencyService.GetPrimaryStoreCurrency();
            Assert.AreEqual(result.Id, currencyUSD.Id);
        }

        [TestMethod()]
        public async Task InsertCurrency_ValidArgument()
        {
            await _currencyService.InsertCurrency(new Currency());
            tempCurrencyRepository.Verify(c => c.InsertAsync(It.IsAny<Currency>()), Times.Once);
            tempEventPublisher.Verify(c => c.Publish(It.IsAny<EntityInserted<Currency>>(), default), Times.Once);
        }

        [TestMethod()]
        public async Task UpdateCurrency_ValidArgument()
        {
            await _currencyService.UpdateCurrency(new Currency());
            tempCurrencyRepository.Verify(c => c.UpdateAsync(It.IsAny<Currency>()), Times.Once);
            tempEventPublisher.Verify(c => c.Publish(It.IsAny<EntityUpdated<Currency>>(), default), Times.Once);
        }

        [TestMethod()]
        public async Task DeleteCurrency_ValidArgument()
        {
            await _currencyService.DeleteCurrency(new Currency());
            tempCurrencyRepository.Verify(c => c.DeleteAsync(It.IsAny<Currency>()), Times.Once);
            tempEventPublisher.Verify(c => c.Publish(It.IsAny<EntityDeleted<Currency>>(), default), Times.Once);
        }
    }
}
