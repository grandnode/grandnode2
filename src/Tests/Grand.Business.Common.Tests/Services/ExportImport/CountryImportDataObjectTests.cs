using Grand.Business.Common.Services.Directory;
using Grand.Business.Common.Services.ExportImport;
using Grand.Business.Core.Dto;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Directory;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Common.Tests.Services.ExportImport;

[TestClass]
public class CountryImportDataObjectTests
{
    private MemoryCacheBase _cacheBase;
    private CountryImportDataObject _countryImportDataObject;
    private ICountryService _countryService;
    private Mock<IMediator> _mediatorMock;

    private IRepository<Country> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Country>();

        _mediatorMock = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        var accessControlConfig = new AccessControlConfig();
        _countryService = new CountryService(_repository, _mediatorMock.Object, _cacheBase, accessControlConfig);
        _countryImportDataObject = new CountryImportDataObject(_countryService);
    }

    [TestMethod]
    public async Task ExecuteTest_Import_Insert_NewStates()
    {
        //Arrange
        var country = new Country { TwoLetterIsoCode = "US", Name = "USA" };
        await _repository.InsertAsync(country);
        var states = new List<CountryStatesDto> {
            new() {
                Country = country.TwoLetterIsoCode, Abbreviation = "AK", StateProvinceName = "Alaska", Published = true
            },
            new() {
                Country = country.TwoLetterIsoCode, Abbreviation = "CA", StateProvinceName = "California",
                Published = true, DisplayOrder = 1
            }
        };
        //Act
        await _countryImportDataObject.Execute(states);

        //Assert
        Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == country.Id).StateProvinces.Any());
    }

    [TestMethod]
    public async Task ExecuteTest_Import_Update_States()
    {
        //Arrange
        var country = new Country { TwoLetterIsoCode = "US", Name = "USA" };
        country.StateProvinces.Add(new StateProvince { Abbreviation = "AK" });
        country.StateProvinces.Add(new StateProvince { Abbreviation = "CA" });

        await _repository.InsertAsync(country);
        var states = new List<CountryStatesDto> {
            new() {
                Country = country.TwoLetterIsoCode, Abbreviation = "AK", StateProvinceName = "Alaska", Published = true
            },
            new() {
                Country = country.TwoLetterIsoCode, Abbreviation = "CA", StateProvinceName = "California",
                Published = true, DisplayOrder = 1
            }
        };
        //Act
        await _countryImportDataObject.Execute(states);

        //Assert
        Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == country.Id).StateProvinces.Any(x => x.Published));
    }

    [TestMethod]
    public async Task ExecuteTest_Import_Insert_Update_States()
    {
        //Arrange
        var country = new Country { TwoLetterIsoCode = "US", Name = "USA" };
        country.StateProvinces.Add(new StateProvince { Abbreviation = "AK" });
        country.StateProvinces.Add(new StateProvince { Abbreviation = "CA" });

        await _repository.InsertAsync(country);
        var states = new List<CountryStatesDto> {
            new() {
                Country = country.TwoLetterIsoCode, Abbreviation = "AK", StateProvinceName = "Alaska", Published = true
            },
            new() {
                Country = country.TwoLetterIsoCode, Abbreviation = "CA", StateProvinceName = "California",
                Published = true, DisplayOrder = 1
            },
            new() {
                Country = country.TwoLetterIsoCode, Abbreviation = "HI", StateProvinceName = "Hawaii", Published = true,
                DisplayOrder = 1
            }
        };
        //Act
        await _countryImportDataObject.Execute(states);

        //Assert
        Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == country.Id).StateProvinces.Any(x => x.Published));
        Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == country.Id).StateProvinces.Count == 3);
    }
}