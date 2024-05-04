using Grand.Business.Core.Interfaces.Messages;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers;

[AllowAnonymous]
public class PixelController : BaseController
{
    public virtual async Task<IActionResult> QueuedEmail(
        [FromServices] IQueuedEmailService queuedEmailService,
        string emailId)
    {
        if (!string.IsNullOrEmpty(emailId) && !(Request.GetTypedHeaders().Referer?.ToString() is { } referer &&
                                                referer.ToLowerInvariant()
                                                    .Contains("admin/queuedemail/edit/".ToLowerInvariant())))
        {
            var eueuedEmail = await queuedEmailService.GetQueuedEmailById(emailId);
            if (eueuedEmail is not { ReadOnUtc: null })
                return File(
                    Convert.FromBase64String(
                        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII="),
                    "image/png", "pixel.png");
            eueuedEmail.ReadOnUtc = DateTime.UtcNow;
            await queuedEmailService.UpdateQueuedEmail(eueuedEmail);
        }

        return File(
            Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII="),
            "image/png", "pixel.png");
    }
}