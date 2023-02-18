using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grand.Business.Common.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Domain.Localization;

namespace Grand.Business.Common.Events.Tests
{
    [TestClass()]
    public class LanguageDeletedEventHandlerTests
    {
        private Mock<ILanguageService> _languageServiceMock;
        private Mock<ISettingService> _settingServiceMock;

        private LanguageSettings _languageSettings;

        private LanguageDeletedEventHandler _handler; 

        [TestInitialize()]
        public void Init()
        {
            _languageServiceMock = new Mock<ILanguageService>();
            _settingServiceMock = new Mock<ISettingService>();
            _languageSettings = new LanguageSettings() { DefaultAdminLanguageId = "1" };
            _handler = new LanguageDeletedEventHandler(_languageServiceMock.Object, _settingServiceMock.Object,
                _languageSettings);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Assert
            _languageServiceMock.Setup(x => x.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>())).Returns(Task.FromResult(
                new List<Language> { new Language { Name = "English" }, new Language { Name = "Polish" } } as IList<Language>));

            var notification = new Infrastructure.Events.EntityDeleted<Language>(new Language() { Id = "1" });
            //Act
            await _handler.Handle(notification, CancellationToken.None);

            //Assert
            _settingServiceMock.Verify(x => x.SaveSetting<LanguageSettings>(It.IsAny<LanguageSettings>(), ""), Times.Once());
        }
    }
}