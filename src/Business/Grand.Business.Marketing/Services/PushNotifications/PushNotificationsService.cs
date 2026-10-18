using Grand.Business.Core.Interfaces.Marketing.PushNotifications;
using Grand.Business.Marketing.Utilities;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.PushNotifications;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Grand.Business.Marketing.Services.PushNotifications;

public class PushNotificationsService : IPushNotificationsService
{
    private const string FcmUrl = "https://fcm.googleapis.com/fcm/send";
    private const int MaxRegistrationIdsPerRequest = 1000;
    private readonly HttpClient _httpClient;
    private readonly ILogger<PushNotificationsService> _logger;
    private readonly IMediator _mediator;
    private readonly IRepository<PushMessage> _pushMessagesRepository;
    private readonly PushNotificationsSettings _pushNotificationsSettings;
    private readonly IRepository<PushRegistration> _pushRegistrationRepository;

    public PushNotificationsService(
        IRepository<PushRegistration> pushRegistrationRepository,
        IRepository<PushMessage> pushMessagesRepository,
        IMediator mediator,
        PushNotificationsSettings pushNotificationsSettings,
        ILogger<PushNotificationsService> logger,
        HttpClient httpClient)
    {
        _pushRegistrationRepository = pushRegistrationRepository;
        _pushMessagesRepository = pushMessagesRepository;
        _mediator = mediator;
        _pushNotificationsSettings = pushNotificationsSettings;
        _logger = logger;
        _httpClient = httpClient;
    }

    /// <summary>
    ///     Inserts push receiver
    /// </summary>
    /// <param name="registration"></param>
    public virtual async Task InsertPushReceiver(PushRegistration registration)
    {
        await _pushRegistrationRepository.InsertAsync(registration);

        await _mediator.EntityInserted(registration);
    }

    /// <summary>
    ///     Deletes push receiver
    /// </summary>
    /// <param name="registration"></param>
    public virtual async Task DeletePushReceiver(PushRegistration registration)
    {
        await _pushRegistrationRepository.DeleteAsync(registration);
        await _mediator.EntityDeleted(registration);
    }

    /// <summary>
    ///     Gets push receiver
    /// </summary>
    /// <param name="customerId"></param>
    public virtual async Task<PushRegistration> GetPushReceiverByCustomerId(string customerId)
    {
        return await _pushRegistrationRepository.FirstOrDefaultAsync(
            _pushRegistrationRepository.Table.Where(x => x.CustomerId == customerId));
    }

    /// <summary>
    ///     Updates push receiver
    /// </summary>
    /// <param name="registration"></param>
    public virtual async Task UpdatePushReceiver(PushRegistration registration)
    {
        await _pushRegistrationRepository.UpdateAsync(registration);
        await _mediator.EntityUpdated(registration);
    }

    /// <summary>
    ///     Gets all push receivers
    /// </summary>
    public virtual async Task<List<PushRegistration>> GetAllowedPushReceivers()
    {
        return (await _pushRegistrationRepository.ToListAsync(
            _pushRegistrationRepository.Table.Where(x => x.Allowed))).ToList();
    }

    /// <summary>
    ///     Gets number of customers that accepted push notifications permission popup
    /// </summary>
    public virtual async Task<int> GetAllowedReceivers()
    {
        return await _pushRegistrationRepository.CountAsync(
            _pushRegistrationRepository.Table.Where(x => x.Allowed));
    }

    /// <summary>
    ///     Gets number of customers that denied push notifications permission popup
    /// </summary>
    public virtual async Task<int> GetDeniedReceivers()
    {
        return await _pushRegistrationRepository.CountAsync(
            _pushRegistrationRepository.Table.Where(x => !x.Allowed));
    }

    /// <summary>
    ///     Inserts push message
    /// </summary>
    /// <param name="message"></param>
    public virtual async Task InsertPushMessage(PushMessage message)
    {
        await _pushMessagesRepository.InsertAsync(message);
        await _mediator.EntityInserted(message);
    }

    /// <summary>
    ///     Gets all push messages
    /// </summary>
    public virtual async Task<IPagedList<PushMessage>> GetPushMessages(int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _pushMessagesRepository.Table.OrderByDescending(x => x.SentOn);
        return await _pushMessagesRepository.PagedAsync(query, pageIndex, pageSize);
    }

    /// <summary>
    ///     Gets all push receivers
    /// </summary>
    public virtual async Task<IPagedList<PushRegistration>> GetPushReceivers(int pageIndex = 0,
        int pageSize = int.MaxValue)
    {
        var query = _pushRegistrationRepository.Table.OrderByDescending(x => x.RegisteredOn);
        return await _pushRegistrationRepository.PagedAsync(query, pageIndex, pageSize);
    }

    /// <summary>
    ///     Sends push notification to all receivers
    /// </summary>
    /// <param name="title"></param>
    /// <param name="text"></param>
    /// <param name="pictureUrl"></param>
    /// <param name="registrationIds"></param>
    /// <param name="clickUrl"></param>
    /// <param name="cancellationToken">Cancels the request to the push service</param>
    /// <returns>Bool indicating whether message was sent successfully and string result to display</returns>
    public virtual async Task<(bool, string)> SendPushNotification(string title, string text, string pictureUrl,
        string clickUrl, List<string> registrationIds = null, CancellationToken cancellationToken = default)
    {
        List<string> ids;
        if (registrationIds != null && registrationIds.Any())
        {
            ids = registrationIds;
        }
        else
        {
            var receivers = await GetAllowedPushReceivers();
            if (!receivers.Any()) return (false, "Admin.PushNotifications.Error.NoReceivers");
            ids = receivers.Select(x => x.Token).Distinct().ToList();
        }

        var numberOfReceivers = 0;
        try
        {
            //the push service accepts at most 1000 registration ids per request
            foreach (var batch in ids.Chunk(MaxRegistrationIdsPerRequest))
            {
                var (success, result, delivered) =
                    await SendBatch(title, text, pictureUrl, clickUrl, batch, cancellationToken);
                if (!success) return (false, result);
                numberOfReceivers += delivered;
            }

            return (true, "Admin.PushNotifications.MessageSent");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Error occured while sending push notification");
            return (false, ex.Message);
        }
        finally
        {
            //record what was delivered, also when a later batch failed or the caller cancelled
            if (numberOfReceivers > 0)
                await InsertPushMessage(new PushMessage {
                    NumberOfReceivers = numberOfReceivers,
                    SentOn = DateTime.UtcNow,
                    Text = text,
                    Title = title
                });
        }
    }

    private async Task<(bool success, string result, int delivered)> SendBatch(string title, string text,
        string pictureUrl, string clickUrl, string[] registrationIds, CancellationToken cancellationToken)
    {
        var data = new {
            registration_ids = registrationIds,
            notification = new {
                body = text,
                title,
                icon = pictureUrl,
                click_action = clickUrl
            }
        };

        var json = JsonSerializer.Serialize(data);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, FcmUrl);
        httpRequest.Headers.Add("Authorization", $"key = {_pushNotificationsSettings.PrivateApiKey}");
        httpRequest.Headers.Add("Sender", $"id = {_pushNotificationsSettings.SenderId}");

        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Error occured while sending push notification {ResponseString}", responseString);
            return (false, responseString, 0);
        }

        var responseMessage = JsonSerializer.Deserialize<JsonResponse>(responseString);
        return responseMessage == null
            ? (false, "PushNotifications.ResponseMessage.Empty", 0)
            : (true, string.Empty, responseMessage.success);
    }

    /// <summary>
    ///     Sends push notification to specified customer
    /// </summary>
    /// <param name="title"></param>
    /// <param name="text"></param>
    /// <param name="pictureUrl"></param>
    /// <param name="customerId"></param>
    /// <param name="clickUrl"></param>
    /// <param name="cancellationToken">Cancels the request to the push service</param>
    /// <returns>Bool indicating whether message was sent successfully and string result to display</returns>
    public virtual async Task<(bool, string)> SendPushNotification(string title, string text, string pictureUrl,
        string customerId, string clickUrl, CancellationToken cancellationToken = default)
    {
        var receiver = await GetPushReceiverByCustomerId(customerId);
        if (receiver == null) return (false, "Admin.PushNotifications.Error.NoReceivers");

        return await SendPushNotification(title, text, pictureUrl, clickUrl, [receiver.Token], cancellationToken);
    }

    /// <summary>
    ///     Gets all push receivers
    /// </summary>
    /// <param name="id">Ident</param>
    public virtual Task<PushRegistration> GetPushReceiver(string id)
    {
        return _pushRegistrationRepository.GetByIdAsync(id);
    }
}