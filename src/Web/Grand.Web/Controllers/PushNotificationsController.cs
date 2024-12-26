﻿using Grand.Business.Core.Interfaces.Marketing.PushNotifications;
using Grand.Domain.PushNotifications;
using Grand.Infrastructure;
using Grand.SharedKernel.Attributes;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers;

public class PushNotificationsController : BasePublicController
{
    private readonly IPushNotificationsService _pushNotificationsService;
    private readonly IWorkContextAccessor _workContextAccessor;

    public PushNotificationsController(IWorkContextAccessor workContextAccessor, IPushNotificationsService pushNotificationsService)
    {
        _workContextAccessor = workContextAccessor;
        _pushNotificationsService = pushNotificationsService;
    }

    [IgnoreApi]
    [HttpPost]
    public virtual async Task<IActionResult> ProcessRegistration(bool success, string value)
    {
        if (success)
        {
            var toUpdate = await _pushNotificationsService.GetPushReceiverByCustomerId(_workContextAccessor.WorkContext.CurrentCustomer.Id);

            if (toUpdate == null)
            {
                await _pushNotificationsService.InsertPushReceiver(new PushRegistration {
                    CustomerId = _workContextAccessor.WorkContext.CurrentCustomer.Id,
                    Token = value,
                    RegisteredOn = DateTime.UtcNow,
                    Allowed = true
                });
            }
            else
            {
                toUpdate.Token = value;
                toUpdate.RegisteredOn = DateTime.UtcNow;
                toUpdate.Allowed = true;
                await _pushNotificationsService.UpdatePushReceiver(toUpdate);
            }
        }
        else
        {
            if (value != "Permission denied") return new JsonResult("");
            var toUpdate = await _pushNotificationsService.GetPushReceiverByCustomerId(_workContextAccessor.WorkContext.CurrentCustomer.Id);

            if (toUpdate == null)
            {
                await _pushNotificationsService.InsertPushReceiver(new PushRegistration {
                    CustomerId = _workContextAccessor.WorkContext.CurrentCustomer.Id,
                    Token = "[DENIED]",
                    RegisteredOn = DateTime.UtcNow,
                    Allowed = false
                });
            }
            else
            {
                toUpdate.Token = "[DENIED]";
                toUpdate.RegisteredOn = DateTime.UtcNow;
                toUpdate.Allowed = false;
                await _pushNotificationsService.UpdatePushReceiver(toUpdate);
            }
        }

        return new JsonResult("");
    }
}