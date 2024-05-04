using Grand.Business.Marketing.Services.PushNotifications;
using Grand.Business.Marketing.Utilities;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.PushNotifications;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace Grand.Business.Marketing.Tests.Services.PushNotifications;

[TestClass]
public class PushNotificationsServiceTests
{
    private Mock<ILogger<PushNotificationsService>> _loggerMock;
    private Mock<IMediator> _mediatorMock;
    private PushNotificationsService _pushNotificationsService;
    private IRepository<PushMessage> _repositoryPushMessage;
    private IRepository<PushRegistration> _repositoryPushRegistration;


    [TestInitialize]
    public void Init()
    {
        _repositoryPushRegistration = new MongoDBRepositoryTest<PushRegistration>();
        _repositoryPushMessage = new MongoDBRepositoryTest<PushMessage>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<PushNotificationsService>>();

        var mockMessageHandler = new Mock<HttpMessageHandler>();

        var output = JsonSerializer.Serialize(new JsonResponse { success = 1, failure = 0, canonical_ids = 1 });

        mockMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(output)
            });
        var httpClient = new HttpClient(mockMessageHandler.Object);

        _pushNotificationsService = new PushNotificationsService(_repositoryPushRegistration, _repositoryPushMessage,
            _mediatorMock.Object, new PushNotificationsSettings(),
            _loggerMock.Object, httpClient);
    }

    [TestMethod]
    public async Task InsertPushReceiverTest()
    {
        //Act
        await _pushNotificationsService.InsertPushReceiver(new PushRegistration());
        //Assert
        Assert.IsTrue(_repositoryPushRegistration.Table.Any());
    }

    [TestMethod]
    public async Task DeletePushReceiverTest()
    {
        //Arrange
        var pushRegistration = new PushRegistration();
        await _pushNotificationsService.InsertPushReceiver(pushRegistration);

        //Act
        await _pushNotificationsService.DeletePushReceiver(pushRegistration);

        //Assert
        Assert.IsFalse(_repositoryPushRegistration.Table.Any());
    }

    [TestMethod]
    public async Task GetPushReceiverByCustomerIdTest()
    {
        //Arrange
        var pushRegistration = new PushRegistration { CustomerId = "1" };
        await _pushNotificationsService.InsertPushReceiver(pushRegistration);

        //Act
        var result = await _pushNotificationsService.GetPushReceiverByCustomerId(pushRegistration.CustomerId);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("1", result.CustomerId);
    }

    [TestMethod]
    public async Task UpdatePushReceiverTest()
    {
        //Arrange
        var pushRegistration = new PushRegistration { CustomerId = "1" };
        await _pushNotificationsService.InsertPushReceiver(pushRegistration);

        //Act
        pushRegistration.Allowed = true;
        await _pushNotificationsService.UpdatePushReceiver(pushRegistration);

        //Assert
        Assert.IsTrue(_repositoryPushRegistration.Table.FirstOrDefault(x => x.Id == pushRegistration.Id).Allowed);
    }

    [TestMethod]
    public async Task GetPushReceiversTest()
    {
        //Arrange
        await _pushNotificationsService.InsertPushReceiver(new PushRegistration { Allowed = true });
        await _pushNotificationsService.InsertPushReceiver(new PushRegistration { Allowed = true });

        //Act
        var result = await _pushNotificationsService.GetPushReceivers();

        //Assert
        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public async Task GetAllowedReceiversTest()
    {
        //Arrange
        await _pushNotificationsService.InsertPushReceiver(new PushRegistration());
        await _pushNotificationsService.InsertPushReceiver(new PushRegistration { Allowed = true });

        //Act
        var result = await _pushNotificationsService.GetAllowedReceivers();

        //Assert
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public async Task GetDeniedReceiversTest()
    {
        //Arrange
        await _pushNotificationsService.InsertPushReceiver(new PushRegistration());
        await _pushNotificationsService.InsertPushReceiver(new PushRegistration { Allowed = true });

        //Act
        var result = await _pushNotificationsService.GetDeniedReceivers();

        //Assert
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public async Task InsertPushMessageTest()
    {
        //Act
        await _pushNotificationsService.InsertPushMessage(new PushMessage());
        //Assert
        Assert.IsFalse(_repositoryPushRegistration.Table.Any());
    }

    [TestMethod]
    public async Task GetPushMessagesTest()
    {
        //Arrange
        await _pushNotificationsService.InsertPushMessage(new PushMessage());
        await _pushNotificationsService.InsertPushMessage(new PushMessage());
        await _pushNotificationsService.InsertPushMessage(new PushMessage());

        //Act
        var result = await _pushNotificationsService.GetPushMessages();

        //Assert
        Assert.AreEqual(3, result.Count);
    }

    [TestMethod]
    public async Task GetPushReceiversTest1()
    {
        //Arrange
        await _repositoryPushRegistration.InsertAsync(new PushRegistration { Allowed = true });
        await _repositoryPushRegistration.InsertAsync(new PushRegistration { Allowed = true });
        await _repositoryPushRegistration.InsertAsync(new PushRegistration { Allowed = true });

        //Act
        var result = await _pushNotificationsService.GetPushReceivers();

        //Assert
        Assert.AreEqual(3, result.Count);
    }

    [TestMethod]
    public async Task SendPushNotificationTest()
    {
        //Arrange
        await _repositoryPushRegistration.InsertAsync(new PushRegistration { Allowed = true });
        await _repositoryPushRegistration.InsertAsync(new PushRegistration { Allowed = true });
        await _repositoryPushRegistration.InsertAsync(new PushRegistration { Allowed = true });
        //Act
        var result = await _pushNotificationsService.SendPushNotification("my title", "sample text", "", "");
        //Assert
        Assert.IsTrue(result.Item1);
    }


    [TestMethod]
    public async Task GetPushReceiverTest()
    {
        //Arrange
        await _repositoryPushRegistration.InsertAsync(new PushRegistration { Id = "1", Allowed = true });
        await _repositoryPushRegistration.InsertAsync(new PushRegistration { Allowed = true });
        await _repositoryPushRegistration.InsertAsync(new PushRegistration { Allowed = true });

        //Act
        var result = await _pushNotificationsService.GetPushReceiver("1");

        //Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Allowed);
    }
}