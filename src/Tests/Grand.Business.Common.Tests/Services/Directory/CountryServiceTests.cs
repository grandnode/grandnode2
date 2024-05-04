using Grand.Business.Common.Services.Directory;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Directory;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Common.Tests.Services.Directory;

[TestClass]
public class CountryServiceTests
{
    private MemoryCacheBase _cacheBase;

    private CountryService _countryService;
    private Mock<IMediator> _mediatorMock;
    private IRepository<Country> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Country>();

        _mediatorMock = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _countryService = new CountryService(_repository, _mediatorMock.Object, _cacheBase, new AccessControlConfig());
    }

    [TestMethod]
    public async Task GetAllCountriesTest()
    {
        //Arrange
        await _countryService.InsertCountry(new Country { Published = true });
        await _countryService.InsertCountry(new Country { Published = true });
        //Act
        var result = await _countryService.GetAllCountries();
        //Assert
        Assert.IsTrue(result.Count > 0);
    }

    [TestMethod]
    public async Task GetAllCountriesForBillingTest()
    {
        //Arrange
        await _countryService.InsertCountry(new Country { Published = true, AllowsBilling = true });
        await _countryService.InsertCountry(new Country { Published = true });
        //Act
        var result = await _countryService.GetAllCountriesForBilling();
        //Assert
        Assert.IsTrue(result.Count == 1);
    }

    [TestMethod]
    public async Task GetAllCountriesForShippingTest()
    {
        //Arrange
        await _countryService.InsertCountry(new Country { Published = true, AllowsShipping = true });
        await _countryService.InsertCountry(new Country { Published = true });
        //Act
        var result = await _countryService.GetAllCountriesForShipping();
        //Assert
        Assert.IsTrue(result.Count == 1);
    }

    [TestMethod]
    public async Task GetCountryByIdTest()
    {
        //Arrange
        var country = new Country();
        await _repository.InsertAsync(country);
        await _repository.InsertAsync(new Country { Published = true, AllowsShipping = true });
        await _repository.InsertAsync(new Country { Published = true });
        //Act
        var result = await _countryService.GetCountryById(country.Id);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetCountriesByIdsTest()
    {
        //Arrange
        var country = new Country();
        await _repository.InsertAsync(country);
        await _repository.InsertAsync(new Country { Published = true, AllowsShipping = true });
        await _repository.InsertAsync(new Country { Published = true });
        //Act
        var result = await _countryService.GetCountriesByIds([country.Id]);
        //Assert
        Assert.IsTrue(result.Count == 1);
    }

    [TestMethod]
    public async Task GetCountryByTwoLetterIsoCodeTest()
    {
        //Arrange
        var country = new Country { TwoLetterIsoCode = "US" };
        await _repository.InsertAsync(country);
        await _repository.InsertAsync(new Country { Published = true, AllowsShipping = true });
        await _repository.InsertAsync(new Country { Published = true });
        //Act
        var result = await _countryService.GetCountryByTwoLetterIsoCode(country.TwoLetterIsoCode);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetCountryByThreeLetterIsoCodeTest()
    {
        //Arrange
        var country = new Country { ThreeLetterIsoCode = "USA" };
        await _repository.InsertAsync(country);
        await _repository.InsertAsync(new Country { Published = true, AllowsShipping = true });
        await _repository.InsertAsync(new Country { Published = true });
        //Act
        var result = await _countryService.GetCountryByThreeLetterIsoCode(country.ThreeLetterIsoCode);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task InsertCountryTest()
    {
        //Act
        await _countryService.InsertCountry(new Country());
        //Assert
        Assert.IsTrue(_repository.Table.Any());
    }

    [TestMethod]
    public async Task UpdateCountryTest()
    {
        //Arrange
        var country = new Country { ThreeLetterIsoCode = "USA", Name = "United States" };
        await _repository.InsertAsync(country);
        //Act
        country.Name = "USA";
        await _countryService.UpdateCountry(country);
        //Assert
        Assert.AreEqual("USA", _repository.Table.FirstOrDefault(x => x.Id == country.Id).Name);
    }

    [TestMethod]
    public async Task DeleteCountryTest()
    {
        //Arrange
        var country = new Country { ThreeLetterIsoCode = "USA", Name = "United States" };
        await _repository.InsertAsync(country);
        //Act
        await _countryService.DeleteCountry(country);
        //Assert
        Assert.IsFalse(_repository.Table.Any());
    }

    [TestMethod]
    public async Task GetStateProvincesByCountryIdTest()
    {
        //Arrange
        var country = new Country { ThreeLetterIsoCode = "USA", Name = "United States" };
        country.StateProvinces.Add(new StateProvince());
        await _repository.InsertAsync(country);
        //Act
        var result = await _countryService.GetStateProvincesByCountryId(country.Id);
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task InsertStateProvinceTest()
    {
        //Arrange
        var country = new Country { ThreeLetterIsoCode = "USA", Name = "United States" };
        await _repository.InsertAsync(country);
        //Act
        await _countryService.InsertStateProvince(new StateProvince(), country.Id);
        //Assert
        var result = await _countryService.GetStateProvincesByCountryId(country.Id);
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task UpdateStateProvinceTest()
    {
        //Arrange
        var country = new Country { ThreeLetterIsoCode = "USA", Name = "United States" };
        await _repository.InsertAsync(country);
        var state = new StateProvince();
        await _countryService.InsertStateProvince(state, country.Id);
        //Act
        state.Name = "TEST";
        await _countryService.UpdateStateProvince(state, country.Id);
        //Assert
        var result = await _countryService.GetStateProvincesByCountryId(country.Id);
        Assert.IsTrue(result.Any());
        Assert.AreEqual("TEST", result.FirstOrDefault().Name);
    }

    [TestMethod]
    public async Task DeleteStateProvinceTest()
    {
        //Arrange
        var country = new Country { ThreeLetterIsoCode = "USA", Name = "United States" };
        await _repository.InsertAsync(country);
        var state = new StateProvince();
        await _countryService.InsertStateProvince(state, country.Id);
        //Act
        await _countryService.DeleteStateProvince(state, country.Id);
        //Assert
        var result = await _countryService.GetStateProvincesByCountryId(country.Id);
        Assert.IsFalse(result.Any());
    }
}