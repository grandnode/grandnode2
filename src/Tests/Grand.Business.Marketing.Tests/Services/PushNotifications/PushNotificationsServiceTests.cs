using Grand.Business.Marketing.Services.PushNotifications;
using Grand.Business.Marketing.Utilities;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.PushNotifications;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using System.Net;
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
        Assert.IsNotEmpty(_repositoryPushRegistration.Table);
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
        Assert.IsEmpty(_repositoryPushRegistration.Table);
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
        var result = await _pushNotificationsService.GetAllowedPushReceivers();

        //Assert
        Assert.HasCount(2, result);
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
        Assert.IsEmpty(_repositoryPushRegistration.Table);
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
        Assert.HasCount(3, result);
    }

    [TestMethod]
    public async Task GetPushReceiversTest1()
    {
        //Arrange
        await _repositoryPushRegistration.InsertAsync(new PushRegistration { Allowed = true });
        await _repositoryPushRegistration.InsertAsync(new PushRegistration { Allowed = true });
        await _repositoryPushRegistration.InsertAsync(new PushRegistration { Allowed = true });

        //Act
        var result = await _pushNotificationsService.GetAllowedPushReceivers();

        //Assert
        Assert.HasCount(3, result);
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
    public async Task SendPushNotification_Cancelled_ThrowsAndStoresNoMessage()
    {
        //Arrange
        var hangingHandler = new Mock<HttpMessageHandler>();
        hangingHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage _, CancellationToken ct) => {
                await Task.Delay(Timeout.Infinite, ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
        var service = new PushNotificationsService(_repositoryPushRegistration, _repositoryPushMessage,
            _mediatorMock.Object, new PushNotificationsSettings(), _loggerMock.Object,
            new HttpClient(hangingHandler.Object));
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        //Act
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.SendPushNotification("my title", "sample text", "", "", ["token"], cts.Token)
                .WaitAsync(TimeSpan.FromSeconds(10)));

        //Assert
        Assert.IsEmpty(_repositoryPushMessage.Table);
    }

    //answers every request as delivered to all of its registration ids, and keeps the ids of each request
    private PushNotificationsService ServiceRecordingRequests(List<string[]> requests)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage request, CancellationToken ct) => {
                using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
                var ids = body.RootElement.GetProperty("registration_ids").EnumerateArray()
                    .Select(x => x.GetString()).ToArray();
                requests.Add(ids);
                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent(JsonSerializer.Serialize(new JsonResponse { success = ids.Length }))
                };
            });
        return new PushNotificationsService(_repositoryPushRegistration, _repositoryPushMessage,
            _mediatorMock.Object, new PushNotificationsSettings(), _loggerMock.Object,
            new HttpClient(handler.Object));
    }

    [TestMethod]
    public async Task SendPushNotification_ManyReceivers_SendsBatchesOfAtMostThousand()
    {
        //Arrange
        for (var i = 0; i < 2500; i++)
            await _repositoryPushRegistration.InsertAsync(new PushRegistration { Allowed = true, Token = $"t{i}" });
        var requests = new List<string[]>();
        var service = ServiceRecordingRequests(requests);

        //Act
        var result = await service.SendPushNotification("my title", "sample text", "", "");

        //Assert
        Assert.IsTrue(result.Item1);
        CollectionAssert.AreEqual(new[] { 1000, 1000, 500 }, requests.Select(x => x.Length).ToArray());
        Assert.AreEqual(2500, requests.SelectMany(x => x).Distinct().Count());
        Assert.AreEqual(2500, _repositoryPushMessage.Table.Single().NumberOfReceivers);
    }

    [TestMethod]
    public async Task SendPushNotification_ToCustomer_SendsCustomerToken()
    {
        //Arrange
        await _repositoryPushRegistration.InsertAsync(new PushRegistration {
            CustomerId = "c1", Allowed = true, Token = "customer-token"
        });
        var requests = new List<string[]>();
        var service = ServiceRecordingRequests(requests);

        //Act
        var result = await service.SendPushNotification("my title", "sample text", "", "c1", "");

        //Assert
        Assert.IsTrue(result.Item1);
        CollectionAssert.AreEqual(new[] { "customer-token" }, requests.Single());
    }

    [TestMethod]
    public async Task SendPushNotification_ToCustomerWithoutRegistration_Fails()
    {
        var requests = new List<string[]>();
        var service = ServiceRecordingRequests(requests);

        var result = await service.SendPushNotification("my title", "sample text", "", "unknown", "");

        Assert.IsFalse(result.Item1);
        Assert.IsEmpty(requests);
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