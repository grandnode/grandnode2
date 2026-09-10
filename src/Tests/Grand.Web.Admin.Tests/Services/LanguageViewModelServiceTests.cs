using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Localization;
using Grand.Web.AdminShared.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class LanguageViewModelServiceTests
{
    private Mock<ILanguageService> _languageServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private Mock<ICurrencyService> _currencyServiceMock;
    private Mock<IWebHostEnvironment> _hostingEnvironmentMock;
    private LanguageViewModelService _service;

    [TestInitialize]
    public void Setup()
    {
        _languageServiceMock = new Mock<ILanguageService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _currencyServiceMock = new Mock<ICurrencyService>();
        _hostingEnvironmentMock = new Mock<IWebHostEnvironment>();

        _service = new LanguageViewModelService(
            _languageServiceMock.Object,
            _translationServiceMock.Object,
            _currencyServiceMock.Object,
            _hostingEnvironmentMock.Object);
    }

    [TestMethod]
    public async Task ValidateLanguageUnpublish_StillPublished_AlwaysCanProceed()
    {
        var (canProceed, message) = await _service.ValidateLanguageUnpublish("language-1", true);

        Assert.IsTrue(canProceed);
        Assert.AreEqual(string.Empty, message);
        _languageServiceMock.Verify(l => l.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ValidateLanguageUnpublish_LastLanguageUnpublished_CannotProceed()
    {
        _languageServiceMock.Setup(l => l.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Language> { new() { Id = "language-1" } });

        var (canProceed, message) = await _service.ValidateLanguageUnpublish("language-1", false);

        Assert.IsFalse(canProceed);
        Assert.AreEqual("At least one published language is required.", message);
    }

    [TestMethod]
    public async Task ValidateLanguageUnpublish_OtherLanguagesExist_CanProceed()
    {
        _languageServiceMock.Setup(l => l.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Language> { new() { Id = "language-1" }, new() { Id = "language-2" } });

        var (canProceed, _) = await _service.ValidateLanguageUnpublish("language-1", false);

        Assert.IsTrue(canProceed);
    }

    [TestMethod]
    public async Task ValidateLanguageDelete_LastLanguage_CannotDelete()
    {
        _languageServiceMock.Setup(l => l.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Language> { new() { Id = "language-1" } });

        var (canDelete, message) = await _service.ValidateLanguageDelete(new Language { Id = "language-1" });

        Assert.IsFalse(canDelete);
        Assert.AreEqual("At least one published language is required.", message);
    }

    [TestMethod]
    public async Task ValidateLanguageDelete_OtherLanguagesExist_CanDelete()
    {
        _languageServiceMock.Setup(l => l.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Language> { new() { Id = "language-1" }, new() { Id = "language-2" } });

        var (canDelete, message) = await _service.ValidateLanguageDelete(new Language { Id = "language-1" });

        Assert.IsTrue(canDelete);
        Assert.AreEqual(string.Empty, message);
    }
}
