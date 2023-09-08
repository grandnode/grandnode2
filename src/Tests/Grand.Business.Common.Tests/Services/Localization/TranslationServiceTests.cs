using Grand.Business.Common.Services.Localization;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Common.Tests.Services.Localization
{
    [TestClass()]
    public class TranslationServiceTests
    {
        private IRepository<TranslationResource> _repository;
        private Mock<IMediator> _mediatorMock;
        private MemoryCacheBase _cacheBase;
        private Mock<IWorkContext> _workContextMock;

        private TranslationService _translationService;

        [TestInitialize()]
        public void Init()
        {
            _repository = new MongoDBRepositoryTest<TranslationResource>();

            _mediatorMock = new Mock<IMediator>();
            _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object, new CacheConfig(){ DefaultCacheTimeMinutes = 1});
            _workContextMock = new Mock<IWorkContext>();
            _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Domain.Stores.Store());
            _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());
            _workContextMock.Setup(c => c.WorkingLanguage).Returns(() => new Language() { Id = "1" });

            _translationService = new TranslationService(_cacheBase, _workContextMock.Object, _repository, _mediatorMock.Object);
        }

        [TestMethod()]
        public async Task GetTranslateResourceByIdTest()
        {
            //Arrange
            var translationResource = new TranslationResource();
            await _repository.InsertAsync(translationResource);

            //Act
            var result = await _translationService.GetTranslateResourceById(translationResource.Id);

            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod()]
        public async Task GetTranslateResourceByNameTest()
        {
            //Arrange
            var translationResource = new TranslationResource() { Name = "name", LanguageId = "1" };
            await _repository.InsertAsync(translationResource);

            //Act
            var result = await _translationService.GetTranslateResourceByName(translationResource.Name, translationResource.LanguageId);

            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod()]
        public async Task GetAllResourcesTest()
        {
            //Arrange
            await _repository.InsertAsync(new TranslationResource() { LanguageId = "1" });
            await _repository.InsertAsync(new TranslationResource() { LanguageId = "1" });
            await _repository.InsertAsync(new TranslationResource() { LanguageId = "2" });

            //Act
            var result = _translationService.GetAllResources("1");

            //Assert
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod()]
        public async Task InsertTranslateResourceTest()
        {
            //Arrange
            var translationResource = new TranslationResource() { Name = "name", LanguageId = "1" };

            //Act
            await _translationService.InsertTranslateResource(translationResource);

            //Assert
            Assert.AreEqual(1, _repository.Table.Count());
        }

        [TestMethod()]
        public async Task UpdateTranslateResourceTest()
        {
            //Arrange
            var translationResource = new TranslationResource() { Name = "name", Value = "value", LanguageId = "1" };
            await _translationService.InsertTranslateResource(translationResource);

            //Act
            translationResource.Name = "name2";
            await _translationService.UpdateTranslateResource(translationResource);

            //Assert
            Assert.AreEqual(translationResource.Name, _repository.Table.FirstOrDefault(x=>x.Id == translationResource.Id).Name);
        }

        [TestMethod()]
        public async Task DeleteTranslateResourceTest()
        {
            //Arrange
            var translationResource = new TranslationResource() { Name = "name", Value = "value", LanguageId = "1" };
            await _translationService.InsertTranslateResource(translationResource);

            //Act
            await _translationService.DeleteTranslateResource(translationResource);

            //Assert
            Assert.AreEqual(0, _repository.Table.Count());
        }

        [TestMethod()]
        public async Task GetResourceTest()
        {
            //Arrange
            await _repository.InsertAsync(new TranslationResource() { Name = "name1", Value = "value1", LanguageId = "1" });
            await _repository.InsertAsync(new TranslationResource() { Name = "name2", Value = "value2", LanguageId = "1" });
            await _repository.InsertAsync(new TranslationResource() { Name = "name3", Value = "value3", LanguageId = "2" });

            //Act
            var result = _translationService.GetResource("name1");

            //Assert
            Assert.AreEqual("value1", result);
        }
       

        [TestMethod()]
        public async Task ExportResourcesToXmlTest()
        {
            //Arrange
            await _repository.InsertAsync(new TranslationResource() { Name = "name1", Value = "value1", LanguageId = "1" });

            //Act
            var result = await _translationService.ExportResourcesToXml(new Language() { Id = "1", Name = "en"});

            //Assert
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?><Language Name=\"en\"><Resource Name=\"name1\" Area=\"Common\"><Value>value1</Value></Resource></Language>", result);
        }

        [TestMethod()]
        public async Task ImportResourcesFromXmlTest()
        {
            //Act
            await _translationService.ImportResourcesFromXml(new Language() { Id = "1", Name = "en" }, "<?xml version=\"1.0\" encoding=\"utf-16\"?><Language Name=\"en\"><Resource Name=\"name1\" Area=\"Common\"><Value>value1</Value></Resource></Language>");

            //Assert
            Assert.IsTrue(_repository.Table.Any());
        }

        [TestMethod()]
        public async Task ImportResourcesFromXmlInstallTest()
        {
            //Act
            await _translationService.ImportResourcesFromXmlInstall(new Language() { Id = "1", Name = "en" }, "<?xml version=\"1.0\" encoding=\"utf-16\"?><Language Name=\"en\"><Resource Name=\"name1\" Area=\"Common\"><Value>value1</Value></Resource></Language>");

            //Assert
            Assert.IsTrue(_repository.Table.Any());
        }
    }
}