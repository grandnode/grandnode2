using Grand.Business.Common.Events;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Localization;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Common.Tests.Events;

[TestClass]
public class LanguageDeletedEventHandlerTests
{
    private LanguageDeletedEventHandler _handler;
    private Mock<ILanguageService> _languageServiceMock;

    private LanguageSettings _languageSettings;
    private Mock<ISettingService> _settingServiceMock;

    [TestInitialize]
    public void Init()
    {
        _languageServiceMock = new Mock<ILanguageService>();
        _settingServiceMock = new Mock<ISettingService>();
        _languageSettings = new LanguageSettings { DefaultAdminLanguageId = "1" };
        _handler = new LanguageDeletedEventHandler(_languageServiceMock.Object, _settingServiceMock.Object,
            _languageSettings);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Assert
        _languageServiceMock.Setup(x => x.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>())).Returns(
            Task.FromResult(
                new List<Language> { new() { Name = "English" }, new() { Name = "Polish" } } as IList<Language>));

        var notification = new EntityDeleted<Language>(new Language { Id = "1" });
        //Act
        await _handler.Handle(notification, CancellationToken.None);

        //Assert
        _settingServiceMock.Verify(x => x.SaveSetting(It.IsAny<LanguageSettings>(), ""), Times.Once());
    }
}