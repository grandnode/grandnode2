﻿using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Interfaces.Marketing.PushNotifications;
using Grand.Business.Marketing.Utilities;
using Grand.Domain;
using Grand.Domain.Data;
using Grand.Domain.PushNotifications;
using Grand.Infrastructure.Extensions;
using MediatR;
using Newtonsoft.Json;
using System.Net.Http;

namespace Grand.Business.Marketing.Services.PushNotifications
{
    public class PushNotificationsService : IPushNotificationsService
    {
        private readonly IRepository<PushRegistration> _pushRegistrationRepository;
        private readonly IRepository<PushMessage> _pushMessagesRepository;
        private readonly IMediator _mediator;
        private readonly PushNotificationsSettings _pushNotificationsSettings;
        private readonly ITranslationService _translationService;
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        private const string FcmUrl = "https://fcm.googleapis.com/fcm/send";

        public PushNotificationsService(
            IRepository<PushRegistration> pushRegistrationRepository,
            IRepository<PushMessage> pushMessagesRepository,
            IMediator mediator,
            PushNotificationsSettings pushNotificationsSettings,
            ITranslationService translationService,
            ILogger logger,
            HttpClient httpClient)
        {
            _pushRegistrationRepository = pushRegistrationRepository;
            _pushMessagesRepository = pushMessagesRepository;
            _mediator = mediator;
            _pushNotificationsSettings = pushNotificationsSettings;
            _translationService = translationService;
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Inserts push receiver
        /// </summary>
        /// <param name="registration"></param>
        public virtual async Task InsertPushReceiver(PushRegistration registration)
        {
            await _pushRegistrationRepository.InsertAsync(registration);

            await _mediator.EntityInserted(registration);
        }

        /// <summary>
        /// Deletes push receiver
        /// </summary>
        /// <param name="registration"></param>
        public virtual async Task DeletePushReceiver(PushRegistration registration)
        {
            await _pushRegistrationRepository.DeleteAsync(registration);
            await _mediator.EntityDeleted(registration);
        }

        /// <summary>
        /// Gets push receiver
        /// </summary>
        /// <param name="customerId"></param>
        public virtual async Task<PushRegistration> GetPushReceiverByCustomerId(string customerId)
        {
            return await Task.FromResult(_pushRegistrationRepository.Table.FirstOrDefault(x => x.CustomerId == customerId));
        }

        /// <summary>
        /// Updates push receiver
        /// </summary>
        /// <param name="registration"></param>
        public virtual async Task UpdatePushReceiver(PushRegistration registration)
        {
            await _pushRegistrationRepository.UpdateAsync(registration);
            await _mediator.EntityUpdated(registration);
        }

        /// <summary>
        /// Gets all push receivers
        /// </summary>
        public virtual async Task<List<PushRegistration>> GetPushReceivers()
        {
            return await Task.FromResult(_pushRegistrationRepository.Table.Where(x => x.Allowed).ToList());
        }

        /// <summary>
        /// Gets number of customers that accepted push notifications permission popup
        /// </summary>
        public virtual async Task<int> GetAllowedReceivers()
        {
            return await Task.FromResult(_pushRegistrationRepository.Table.Count(x => x.Allowed));
        }

        /// <summary>
        /// Gets number of customers that denied push notifications permission popup
        /// </summary>
        public virtual async Task<int> GetDeniedReceivers()
        {
            return await Task.FromResult(_pushRegistrationRepository.Table.Count(x => !x.Allowed));
        }

        /// <summary>
        /// Inserts push message
        /// </summary>
        /// <param name="message"></param>
        public virtual async Task InsertPushMessage(PushMessage message)
        {
            await _pushMessagesRepository.InsertAsync(message);
            await _mediator.EntityInserted(message);
        }

        /// <summary>
        /// Gets all push messages
        /// </summary>
        public virtual async Task<IPagedList<PushMessage>> GetPushMessages(int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = _pushMessagesRepository.Table.OrderByDescending(x => x.SentOn);
            return await Task.FromResult(new PagedList<PushMessage>(query, pageIndex, pageSize));
        }

        /// <summary>
        /// Gets all push receivers
        /// </summary>
        public virtual async Task<IPagedList<PushRegistration>> GetPushReceivers(int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = _pushRegistrationRepository.Table.OrderByDescending(x => x.RegisteredOn);
            return await Task.FromResult(new PagedList<PushRegistration>(query, pageIndex, pageSize));
        }

        /// <summary>
        /// Sends push notification to all receivers
        /// </summary>
        /// <param name="title"></param>
        /// <param name="text"></param>
        /// <param name="pictureUrl"></param>
        /// <param name="registrationIds"></param>
        /// <param name="clickUrl"></param>
        /// <returns>Bool indicating whether message was sent successfully and string result to display</returns>
        public virtual async Task<(bool, string)> SendPushNotification(string title, string text, string pictureUrl, string clickUrl, List<string> registrationIds = null)
        {
            var ids = new List<string>();

            if (registrationIds != null && registrationIds.Any())
            {
                ids = registrationIds;
            }
            else
            {
                var receivers = await GetPushReceivers();
                if (!receivers.Any())
                {
                    return (false, _translationService.GetResource("Admin.PushNotifications.Error.NoReceivers"));
                }

                const int batchSize = 1000;
                for (var batch = 0; batch <= Math.Round((double)(receivers.Count / batchSize), 0, MidpointRounding.ToEven); batch++)
                {
                    var t = receivers.Skip(batch * batchSize).Take(batchSize);
                    foreach (var receiver in receivers.Where(receiver => !ids.Contains(receiver.Token)))
                    {
                        ids.Add(receiver.Token);
                    }
                }
            }

            var data = new
            {
                registration_ids = ids,
                notification = new
                {
                    body = text,
                    title = title,
                    icon = pictureUrl,
                    click_action = clickUrl
                }
            };

            var json = JsonConvert.SerializeObject(data);

            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, FcmUrl);
                httpRequest.Headers.Add("Authorization", $"key = {_pushNotificationsSettings.PrivateApiKey}");
                httpRequest.Headers.Add("Sender", $"id = {_pushNotificationsSettings.SenderId}");

                httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(httpRequest);
                var responseString = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    await _logger.InsertLog(Domain.Logging.LogLevel.Error, "Error occured while sending push notification.", responseString);
                    return (false, responseString);
                }

                var responseMessage = JsonConvert.DeserializeObject<JsonResponse>(responseString);
                if (responseMessage == null) return (false, "PushNotifications.ResponseMessage.Empty");
                    
                await InsertPushMessage(new PushMessage {
                    NumberOfReceivers = responseMessage.success,
                    SentOn = DateTime.UtcNow,
                    Text = text,
                    Title = title
                });

                return (true, string.Format(_translationService.GetResource("Admin.PushNotifications.MessageSent"), responseMessage.success, responseMessage.failure));
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Sends push notification to specified customer
        /// </summary>
        /// <param name="title"></param>
        /// <param name="text"></param>
        /// <param name="pictureUrl"></param>
        /// <param name="customerId"></param>
        /// <param name="clickUrl"></param>
        /// <returns>Bool indicating whether message was sent successfully and string result to display</returns>
        public virtual async Task<(bool, string)> SendPushNotification(string title, string text, string pictureUrl, string customerId, string clickUrl)
        {
            return await SendPushNotification(title, text, pictureUrl, clickUrl, new List<string> { GetPushReceiverByCustomerId(customerId).Id.ToString() });
        }

        /// <summary>
        /// Gets all push receivers
        /// </summary>
        /// <param name="id">Ident</param>
        public virtual Task<PushRegistration> GetPushReceiver(string id)
        {
            return _pushRegistrationRepository.GetByIdAsync(id);
        }
    }
}
