﻿using AutoMapper;
using Grand.Business.Catalog.Services.Brands;
using Grand.Business.Catalog.Services.ExportImport.Dto;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Mapper;
using Grand.Infrastructure.Tests.Caching;
using Grand.Infrastructure.TypeSearch;
using Grand.Infrastructure.TypeSearchers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Services.ExportImport.Tests
{
    [TestClass()]
    public class BrandImportDataObjectTests
    {
        private BrandImportDataObject _brandImportDataObject;
        private IBrandService _brandService;
        private Mock<IWorkContext> _workContextMock;
        private Mock<IPictureService> _pictureServiceMock;
        private Mock<IBrandLayoutService> _brandLayoutServiceMock;
        private Mock<ISlugService> _slugServiceMock;
        private Mock<ILanguageService> _languageServiceMock;


        private IRepository<Brand> _repository;
        private Mock<IMediator> _mediatorMock;
        private MemoryCacheBase _cacheBase;

        [TestInitialize()]
        public void Init()
        {
            InitAutoMapper();

            _repository = new MongoDBRepositoryTest<Brand>();

            _pictureServiceMock = new Mock<IPictureService>();
            _brandLayoutServiceMock = new Mock<IBrandLayoutService>();
            _slugServiceMock = new Mock<ISlugService>();
            _languageServiceMock = new Mock<ILanguageService>();
            _workContextMock = new Mock<IWorkContext>();
            _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Domain.Stores.Store() { Id = "" });
            _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());

            _mediatorMock = new Mock<IMediator>();
            _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object);
            _brandService = new BrandService(_cacheBase, _repository, _workContextMock.Object, _mediatorMock.Object);

            _brandImportDataObject = new BrandImportDataObject(_brandService, _pictureServiceMock.Object, _brandLayoutServiceMock.Object, _slugServiceMock.Object, _languageServiceMock.Object, new Domain.Seo.SeoSettings());
        }

        [TestMethod()]
        public async Task ExecuteTest_Import_Insert()
        {
            //Arrange
            var brands = new List<BrandDto>();
            brands.Add(new BrandDto() { Name = "test1", Published = true });
            brands.Add(new BrandDto() { Name = "test2", Published = true });
            brands.Add(new BrandDto() { Name = "test3", Published = true });
            _brandLayoutServiceMock.Setup(c => c.GetBrandLayoutById(It.IsAny<string>())).Returns(Task.FromResult(new BrandLayout()));
            _brandLayoutServiceMock.Setup(c => c.GetAllBrandLayouts()).Returns(Task.FromResult<IList<BrandLayout>>(new List<BrandLayout>() { new BrandLayout() }));
            _languageServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>())).Returns(Task.FromResult<IList<Language>>(new List<Language>()));
            _slugServiceMock.Setup(c => c.GetBySlug(It.IsAny<string>())).Returns(Task.FromResult(new Domain.Seo.EntityUrl() { Slug = "slug" }));
            //Act
            await _brandImportDataObject.Execute(brands);

            //Assert
            Assert.IsTrue(_repository.Table.Any());
            Assert.AreEqual(3, _repository.Table.Count());

        }
        [TestMethod()]
        public async Task ExecuteTest_Import_Update()
        {
            //Arrange
            var brand1 = new Brand() {
                Name = "insert1"
            };
            await _brandService.InsertBrand(brand1);
            var brand2 = new Brand() {
                Name = "insert2"
            };
            await _brandService.InsertBrand(brand2);
            var brand3 = new Brand() {
                Name = "insert3"
            };
            await _brandService.InsertBrand(brand3);


            var brands = new List<BrandDto>();
            brands.Add(new BrandDto() { Id = brand1.Id, Name = "update1", Published = false, DisplayOrder = 1 });
            brands.Add(new BrandDto() { Id = brand2.Id, Name = "update2", Published = false, DisplayOrder = 2 });
            brands.Add(new BrandDto() { Id = brand3.Id, Name = "update3", Published = false, DisplayOrder = 3 });

            _brandLayoutServiceMock.Setup(c => c.GetBrandLayoutById(It.IsAny<string>())).Returns(Task.FromResult(new BrandLayout()));
            _brandLayoutServiceMock.Setup(c => c.GetAllBrandLayouts()).Returns(Task.FromResult<IList<BrandLayout>>(new List<BrandLayout>() { new BrandLayout() }));
            _languageServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>())).Returns(Task.FromResult<IList<Language>>(new List<Language>()));
            _slugServiceMock.Setup(c => c.GetBySlug(It.IsAny<string>())).Returns(Task.FromResult(new Domain.Seo.EntityUrl() { Slug = "slug" }));
            //Act
            await _brandImportDataObject.Execute(brands);

            //Assert
            Assert.IsTrue(_repository.Table.Any());
            Assert.AreEqual(3, _repository.Table.Count());
            Assert.AreEqual("update3", _repository.Table.FirstOrDefault(x => x.Id == brand3.Id).Name);
            Assert.AreEqual(3, _repository.Table.FirstOrDefault(x => x.Id == brand3.Id).DisplayOrder);
            Assert.AreEqual(false, _repository.Table.FirstOrDefault(x => x.Id == brand3.Id).Published);

        }

        [TestMethod()]
        public async Task ExecuteTest_Import_Insert_Update()
        {
            //Arrange
            var brand3 = new Brand() {
                Name = "insert3"
            };
            await _brandService.InsertBrand(brand3);

            var brands = new List<BrandDto>();
            brands.Add(new BrandDto() { Name = "update1", Published = false, DisplayOrder = 1 });
            brands.Add(new BrandDto() { Name = "update2", Published = false, DisplayOrder = 2 });
            brands.Add(new BrandDto() { Id = brand3.Id, Name = "update3", Published = false, DisplayOrder = 3 });

            _brandLayoutServiceMock.Setup(c => c.GetBrandLayoutById(It.IsAny<string>())).Returns(Task.FromResult(new BrandLayout()));
            _brandLayoutServiceMock.Setup(c => c.GetAllBrandLayouts()).Returns(Task.FromResult<IList<BrandLayout>>(new List<BrandLayout>() { new BrandLayout() }));
            _languageServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>())).Returns(Task.FromResult<IList<Language>>(new List<Language>()));
            _slugServiceMock.Setup(c => c.GetBySlug(It.IsAny<string>())).Returns(Task.FromResult(new Domain.Seo.EntityUrl() { Slug = "slug" }));
            //Act
            await _brandImportDataObject.Execute(brands);

            //Assert
            Assert.IsTrue(_repository.Table.Any());
            Assert.AreEqual(3, _repository.Table.Count());
            Assert.AreEqual("update3", _repository.Table.FirstOrDefault(x => x.Id == brand3.Id).Name);
            Assert.AreEqual(3, _repository.Table.FirstOrDefault(x => x.Id == brand3.Id).DisplayOrder);
            Assert.AreEqual(false, _repository.Table.FirstOrDefault(x => x.Id == brand3.Id).Published);

        }

        private void InitAutoMapper()
        {
            var typeSearcher = new TypeSearcher();
            //find mapper configurations provided by other assemblies
            var mapperConfigurations = typeSearcher.ClassesOfType<IAutoMapperProfile>();

            //create and sort instances of mapper configurations
            var instances = mapperConfigurations
                .Select(mapperConfiguration => (IAutoMapperProfile)Activator.CreateInstance(mapperConfiguration))
                .OrderBy(mapperConfiguration => mapperConfiguration.Order);

            //create AutoMapper configuration
            var config = new MapperConfiguration(cfg =>
            {
                foreach (var instance in instances)
                {
                    cfg.AddProfile(instance.GetType());
                }
            });

            //register automapper
            AutoMapperConfig.Init(config);
        }
    }
}