using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Marketing.Interfaces.PushNotifications;
using Grand.Business.Storage.Interfaces;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Grand.Domain.PushNotifications;
using Grand.Web.Admin.Models.PushNotifications;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.PushNotifications)]
    public class PushNotificationsController : BaseAdminController
    {
        private readonly PushNotificationsSettings _pushNotificationsSettings;
        private readonly ITranslationService _translationService;
        private readonly ISettingService _settingService;
        private readonly IPushNotificationsService _pushNotificationsService;
        private readonly ICustomerService _customerService;
        private readonly IPictureService _pictureService;
        private readonly IDateTimeService _dateTimeService;

        public PushNotificationsController(
            PushNotificationsSettings pushNotificationsSettings,
            ITranslationService translationService,
            ISettingService settingService,
            IPushNotificationsService pushNotificationsService,
            ICustomerService customerService,
            IPictureService pictureService,
            IDateTimeService dateTimeService)
        {
            _pushNotificationsSettings = pushNotificationsSettings;
            _translationService = translationService;
            _settingService = settingService;
            _pushNotificationsService = pushNotificationsService;
            _customerService = customerService;
            _pictureService = pictureService;
            _dateTimeService = dateTimeService;
        }

        public IActionResult Send()
        {
            var model = new PushModel
            {
                MessageText = _translationService.GetResource("Admin.PushNotifications.MessageTextPlaceholder"),
                Title = _translationService.GetResource("Admin.PushNotifications.MessageTitlePlaceholder"),
                PictureId = _pushNotificationsSettings.PictureId,
                ClickUrl = _pushNotificationsSettings.ClickUrl
            };

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        [HttpPost]
        public async Task<IActionResult> Send(PushModel model)
        {
            if (!string.IsNullOrEmpty(_pushNotificationsSettings.PrivateApiKey) && !string.IsNullOrEmpty(model.MessageText))
            {
                _pushNotificationsSettings.PictureId = model.PictureId;
                _pushNotificationsSettings.ClickUrl = model.ClickUrl;
                await _settingService.SaveSetting(_pushNotificationsSettings);
                var pictureUrl = await _pictureService.GetPictureUrl(model.PictureId);
                var result = (await _pushNotificationsService.SendPushNotification(model.Title, model.MessageText, pictureUrl, model.ClickUrl));
                if (result.Item1)
                {
                    Success(result.Item2);
                }
                else
                {
                    Error(result.Item2);
                }
            }
            else
            {
                Error(_translationService.GetResource("PushNotifications.Error.PushApiMessage"));
            }

            return RedirectToAction("Send");
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> Messages()
        {
            var model = new MessagesModel
            {
                Allowed = await _pushNotificationsService.GetAllowedReceivers(),
                Denied = await _pushNotificationsService.GetDeniedReceivers()
            };

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> Receivers()
        {
            var model = new ReceiversModel
            {
                Allowed = await _pushNotificationsService.GetAllowedReceivers(),
                Denied = await _pushNotificationsService.GetDeniedReceivers()
            };

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> PushMessagesList(DataSourceRequest command)
        {
            var messages = await _pushNotificationsService.GetPushMessages(command.Page - 1, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = messages.Select(x => new PushMessageGridModel
                {
                    Id = x.Id,
                    Text = x.Text,
                    Title = x.Title,
                    SentOn = _dateTimeService.ConvertToUserTime(x.SentOn, DateTimeKind.Utc),
                    NumberOfReceivers = x.NumberOfReceivers
                }),
                Total = messages.TotalCount
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> PushReceiversList(DataSourceRequest command)
        {
            var receivers = await _pushNotificationsService.GetPushReceivers(command.Page - 1, command.PageSize);
            var gridModel = new DataSourceResult();
            var list = new List<PushRegistrationGridModel>();
            foreach (var receiver in receivers)
            {
                var gridReceiver = new PushRegistrationGridModel();

                var customer = await _customerService.GetCustomerById(receiver.CustomerId);
                if (customer == null)
                {
                    await _pushNotificationsService.DeletePushReceiver(receiver);
                    continue;
                }

                if (!string.IsNullOrEmpty(customer.Email))
                {
                    gridReceiver.CustomerEmail = customer.Email;
                }
                else
                {
                    gridReceiver.CustomerEmail = _translationService.GetResource("Admin.Customers.Guest");
                }

                gridReceiver.CustomerId = receiver.CustomerId;
                gridReceiver.Id = receiver.Id;
                gridReceiver.RegisteredOn = _dateTimeService.ConvertToUserTime(receiver.RegisteredOn, DateTimeKind.Utc);
                gridReceiver.Token = receiver.Token;
                gridReceiver.Allowed = receiver.Allowed;

                list.Add(gridReceiver);
            }

            gridModel.Data = list;
            gridModel.Total = receivers.TotalCount;

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> DeleteReceiver(string id)
        {
            var receiver = await _pushNotificationsService.GetPushReceiver(id);
            await _pushNotificationsService.DeletePushReceiver(receiver);
            return new JsonResult("");
        }
    }
}
