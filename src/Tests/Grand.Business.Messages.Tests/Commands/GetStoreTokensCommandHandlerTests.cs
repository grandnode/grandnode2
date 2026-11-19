using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Messages.Commands.Handlers.Messages;
using Grand.Business.Messages.Commands.Tokens;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Commands;

[TestClass]
public class GetStoreTokensCommandHandlerTests
{
    [TestMethod]
    public async Task Handle_LinksOfTheMessagesStore()
    {
        //Arrange - sent from a scheduled task or another store's admin, the current store is not the message's
        var settingServiceMock = new Mock<ISettingService>();
        settingServiceMock.Setup(x => x.LoadSetting<StoreInformationSettings>(It.IsAny<string>()))
            .ReturnsAsync(new StoreInformationSettings { FacebookLink = "https://facebook.com/store1" });
        settingServiceMock.Setup(x => x.LoadSetting<StoreInformationSettings>("store2"))
            .ReturnsAsync(new StoreInformationSettings { FacebookLink = "https://facebook.com/store2" });
        var handler = new GetStoreTokensCommandHandler(settingServiceMock.Object);

        //Act
        var result = await handler.Handle(new GetStoreTokensCommand {
            Store = new Store { Id = "store2" }, Language = new Language(), EmailAccount = new EmailAccount()
        }, CancellationToken.None);

        //Assert
        Assert.AreEqual("https://facebook.com/store2", result.FacebookLink);
    }
}
