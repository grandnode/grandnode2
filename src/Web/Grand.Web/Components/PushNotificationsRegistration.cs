using Grand.Domain.PushNotifications;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Models.PushNotifications;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class PushNotificationsRegistration : BaseViewComponent
{
    private readonly PushNotificationsSettings _pushNotificationsSettings;
    private readonly IWorkContext _workContext;

    public PushNotificationsRegistration(IWorkContext workContext, PushNotificationsSettings pushNotificationsSettings)
    {
        _workContext = workContext;
        _pushNotificationsSettings = pushNotificationsSettings;
    }

    public IViewComponentResult Invoke()
    {
        if (!_pushNotificationsSettings.Enabled)
            return Content("");

        var model = new PublicInfoModel {
            PublicApiKey = _pushNotificationsSettings.PublicApiKey,
            SenderId = _pushNotificationsSettings.SenderId,
            AuthDomain = _pushNotificationsSettings.AuthDomain,
            ProjectId = _pushNotificationsSettings.ProjectId,
            StorageBucket = _pushNotificationsSettings.StorageBucket,
            DatabaseUrl = _pushNotificationsSettings.DatabaseUrl,
            AppId = _pushNotificationsSettings.AppId
        };
        if (!_pushNotificationsSettings.Enabled) return Content("");

        if (!_pushNotificationsSettings.AllowGuestNotifications &&
            string.IsNullOrEmpty(_workContext.CurrentCustomer.Email))
            return Content("");

        return View(model);
    }
}