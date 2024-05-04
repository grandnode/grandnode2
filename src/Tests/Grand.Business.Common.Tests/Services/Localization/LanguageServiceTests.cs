using Grand.Business.Common.Services.Localization;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Localization;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Common.Tests.Services.Localization;

[TestClass]
public class LanguageServiceTests
{
    private MemoryCacheBase _cacheBase;

    private LanguageService _languageService;
    private Mock<IMediator> _mediatorMock;
    private IRepository<Language> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Language>();

        _mediatorMock = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _languageService = new LanguageService(_cacheBase, _repository, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task GetAllLanguagesTest()
    {
        //Arrange
        await _repository.InsertAsync(new Language { Published = true });
        await _repository.InsertAsync(new Language { Published = true });
        await _repository.InsertAsync(new Language { Published = true });
        //Act
        var result = await _languageService.GetAllLanguages();
        //Assert
        Assert.AreEqual(3, result.Count);
    }

    [TestMethod]
    public async Task GetLanguageByIdTest()
    {
        //Arrange
        var language = new Language { Published = true, Id = "1" };
        await _repository.InsertAsync(language);
        await _repository.InsertAsync(new Language { Published = true });
        await _repository.InsertAsync(new Language { Published = true });
        //Act
        var result = await _languageService.GetLanguageById(language.Id);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetLanguageByCodeTest()
    {
        //Arrange
        var language = new Language { Published = true, Id = "1", UniqueSeoCode = "en" };
        await _repository.InsertAsync(language);
        await _repository.InsertAsync(new Language { Published = true });
        await _repository.InsertAsync(new Language { Published = true });
        //Act
        var result = await _languageService.GetLanguageByCode(language.UniqueSeoCode);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task InsertLanguageTest()
    {
        //Arrange
        var language = new Language { Published = true, Id = "1", UniqueSeoCode = "en" };
        //Act
        await _languageService.InsertLanguage(language);
        //Assert
        Assert.IsNotNull(_repository.Table.FirstOrDefault(x => x.Id == language.Id));
    }

    [TestMethod]
    public async Task UpdateLanguageTest()
    {
        //Arrange
        var language = new Language { Published = true, Id = "1", UniqueSeoCode = "en" };
        await _languageService.InsertLanguage(language);
        //Act
        language.FlagImageFileName = "en.png";
        await _languageService.UpdateLanguage(language);
        //Assert
        Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == language.Id).FlagImageFileName == "en.png");
    }

    [TestMethod]
    public async Task DeleteLanguageTest()
    {
        //Arrange
        var language = new Language { Published = true, Id = "1", UniqueSeoCode = "en" };
        await _languageService.InsertLanguage(language);
        //Act
        await _languageService.DeleteLanguage(language);
        //Assert
        Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Id == language.Id));
    }
}