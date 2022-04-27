using Grand.Business.Core.Interfaces.Messages;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Grand.Web.Controllers
{
    [AllowAnonymous]
    public class PixelController : BaseController
    {
        public virtual async Task<IActionResult> QueuedEmail(
            [FromServices] IWorkContext workContext,
            [FromServices] IQueuedEmailService queuedEmailService,
            string emailId)
        {
            if (!string.IsNullOrEmpty(emailId))
            {
                if (!Request.Headers[HeaderNames.Referer].ToString().ToLowerInvariant().Contains("admin/queuedemail/edit/".ToLowerInvariant()))
                {
                    var eueuedEmail = await queuedEmailService.GetQueuedEmailById(emailId);
                    if (eueuedEmail != null && !eueuedEmail.ReadOnUtc.HasValue)
                    {
                        eueuedEmail.ReadOnUtc = DateTime.UtcNow;
                        await queuedEmailService.UpdateQueuedEmail(eueuedEmail);
                    }
                }
            }
            return File(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII="), "image/png", "pixel.png");
        }
    }
}
