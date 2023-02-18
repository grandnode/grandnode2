﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grand.Business.Common.Services.ExportImport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Data;
using Grand.Domain.Directory;
using Moq;
using MediatR;
using Grand.Infrastructure.Caching;
using Grand.Data.Tests.MongoDb;
using Grand.Infrastructure.Tests.Caching;
using Grand.Business.Common.Services.Directory;

namespace Grand.Business.Common.Services.ExportImport.Tests
{
    [TestClass()]
    public class CountryImportDataObjectTests
    {
        private CountryImportDataObject _countryImportDataObject;
        private ICountryService _countryService;

        private IRepository<Country> _repository;
        private Mock<IMediator> _mediatorMock;
        private MemoryCacheBase _cacheBase;

        [TestInitialize()]
        public void Init()
        {
            _repository = new MongoDBRepositoryTest<Country>();

            _mediatorMock = new Mock<IMediator>();
            _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object);
            _countryService = new CountryService(_repository, _mediatorMock.Object, _cacheBase);
            _countryImportDataObject = new CountryImportDataObject(_countryService);
        }

        [TestMethod()]
        public async Task ExecuteTest_Import_Insert_NewStates()
        {
            //Arrange
            var country = new Country() { TwoLetterIsoCode = "US", Name = "USA" };
            await _repository.InsertAsync(country);
            var states = new List<CountryStates>() { 
                new CountryStates() { Country = country.TwoLetterIsoCode, Abbreviation = "AK", StateProvinceName = "Alaska", Published = true },
                new CountryStates() { Country = country.TwoLetterIsoCode, Abbreviation = "CA", StateProvinceName = "California", Published = true, DisplayOrder = 1 }
            };
            //Act
            await _countryImportDataObject.Execute(states);

            //Assert
            Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == country.Id).StateProvinces.Any());

        }
        [TestMethod()]
        public async Task ExecuteTest_Import_Update_States()
        {
            //Arrange
            var country = new Country() { TwoLetterIsoCode = "US", Name = "USA" };
            country.StateProvinces.Add(new StateProvince() { Abbreviation = "AK" });
            country.StateProvinces.Add(new StateProvince() { Abbreviation = "CA" });

            await _repository.InsertAsync(country);
            var states = new List<CountryStates>() {
                new CountryStates() { Country = country.TwoLetterIsoCode, Abbreviation = "AK", StateProvinceName = "Alaska", Published = true },
                new CountryStates() { Country = country.TwoLetterIsoCode, Abbreviation = "CA", StateProvinceName = "California", Published = true, DisplayOrder = 1 }
            };
            //Act
            await _countryImportDataObject.Execute(states);

            //Assert
            Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == country.Id).StateProvinces.Any(x=>x.Published));

        }

        [TestMethod()]
        public async Task ExecuteTest_Import_Insert_Update_States()
        {
            //Arrange
            var country = new Country() { TwoLetterIsoCode = "US", Name = "USA" };
            country.StateProvinces.Add(new StateProvince() { Abbreviation = "AK" });
            country.StateProvinces.Add(new StateProvince() { Abbreviation = "CA" });

            await _repository.InsertAsync(country);
            var states = new List<CountryStates>() {
                new CountryStates() { Country = country.TwoLetterIsoCode, Abbreviation = "AK", StateProvinceName = "Alaska", Published = true },
                new CountryStates() { Country = country.TwoLetterIsoCode, Abbreviation = "CA", StateProvinceName = "California", Published = true, DisplayOrder = 1 },
                new CountryStates() { Country = country.TwoLetterIsoCode, Abbreviation = "HI", StateProvinceName = "Hawaii", Published = true, DisplayOrder = 1 }
            };
            //Act
            await _countryImportDataObject.Execute(states);

            //Assert
            Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == country.Id).StateProvinces.Any(x => x.Published));
            Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == country.Id).StateProvinces.Count == 3);

        }
    }
}