using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain;
using Grand.Domain.Messages;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Models.Messages;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.MessageQueue)]
public class QueuedEmailController : BaseAdminController
{
    private readonly IDateTimeService _dateTimeService;
    private readonly IEmailAccountService _emailAccountService;
    private readonly IQueuedEmailService _queuedEmailService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    public QueuedEmailController(IQueuedEmailService queuedEmailService,
        IEmailAccountService emailAccountService,
        IDateTimeService dateTimeService,
        ITranslationService translationService,
        IWorkContext workContext)
    {
        _queuedEmailService = queuedEmailService;
        _emailAccountService = emailAccountService;
        _dateTimeService = dateTimeService;
        _translationService = translationService;
        _workContext = workContext;
    }

    private DataSourceResult PrepareDataSource(IPagedList<QueuedEmail> queuedEmails)
    {
        return new DataSourceResult {
            Data = queuedEmails.Select((Func<QueuedEmail, QueuedEmailModel>)(x =>
            {
                var m = x.ToModel();
                m.PriorityName = x.PriorityId.GetTranslationEnum(_translationService, _workContext);
                m.CreatedOn = _dateTimeService.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);
                if (x.DontSendBeforeDateUtc.HasValue)
                    m.DontSendBeforeDate =
                        _dateTimeService.ConvertToUserTime(x.DontSendBeforeDateUtc.Value, DateTimeKind.Utc);
                if (x.SentOnUtc.HasValue)
                    m.SentOn = _dateTimeService.ConvertToUserTime(x.SentOnUtc.Value, DateTimeKind.Utc);
                if (x.ReadOnUtc.HasValue)
                    m.ReadOn = _dateTimeService.ConvertToUserTime(x.ReadOnUtc.Value, DateTimeKind.Utc);

                m.Body = "";

                return m;
            })),
            Total = queuedEmails.TotalCount
        };
    }

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    public IActionResult List()
    {
        var model = new QueuedEmailListModel {
            //default value
            SearchMaxSentTries = 10
        };
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> QueuedEmailList(DataSourceRequest command, QueuedEmailListModel model)
    {
        DateTime? startDateValue = model.SearchStartDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.SearchStartDate.Value, _dateTimeService.CurrentTimeZone);

        DateTime? endDateValue = model.SearchEndDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.SearchEndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

        var queuedEmails = await _queuedEmailService.SearchEmails(model.SearchFromEmail, model.SearchToEmail,
            model.SearchText,
            startDateValue, endDateValue,
            model.SearchLoadNotSent, false, model.SearchMaxSentTries, true, -1, null,
            command.Page - 1, command.PageSize);

        return Json(PrepareDataSource(queuedEmails));
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> QueuedEmailByReference(DataSourceRequest command, int reference, string objectId)
    {
        var queuedEmails = await _queuedEmailService.SearchEmails(null, null, null,
            null, null,
            false, false, 10, true, reference, objectId,
            command.Page - 1, command.PageSize);

        return Json(PrepareDataSource(queuedEmails));
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var email = await _queuedEmailService.GetQueuedEmailById(id);
        if (email == null)
            //No email found with the specified id
            return RedirectToAction("List");

        var model = email.ToModel();
        model.PriorityName = email.PriorityId.GetTranslationEnum(_translationService, _workContext);
        model.CreatedOn = _dateTimeService.ConvertToUserTime(email.CreatedOnUtc, DateTimeKind.Utc);
        model.EmailAccountName = (await _emailAccountService.GetEmailAccountById(email.EmailAccountId)).DisplayName;
        if (email.SentOnUtc.HasValue)
            model.SentOn = _dateTimeService.ConvertToUserTime(email.SentOnUtc.Value, DateTimeKind.Utc);
        if (email.ReadOnUtc.HasValue)
            model.ReadOn = _dateTimeService.ConvertToUserTime(email.ReadOnUtc.Value, DateTimeKind.Utc);
        if (email.DontSendBeforeDateUtc.HasValue)
            model.DontSendBeforeDate = email.DontSendBeforeDateUtc.ConvertToUserTime(_dateTimeService);
        else model.SendImmediately = true;

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(QueuedEmailModel model, bool continueEditing)
    {
        var email = await _queuedEmailService.GetQueuedEmailById(model.Id);
        if (email == null)
            //No email found with the specified id
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            email = model.ToEntity(email);
            email.DontSendBeforeDateUtc = model.SendImmediately || !model.DontSendBeforeDate.HasValue
                ? null
                : model.DontSendBeforeDate.ConvertToUtcTime(_dateTimeService);
            await _queuedEmailService.UpdateQueuedEmail(email);

            Success(_translationService.GetResource("Admin.System.QueuedEmails.Updated"));
            return continueEditing ? RedirectToAction("Edit", new { id = email.Id }) : RedirectToAction("List");
        }

        //If we got this far, something failed, redisplay form
        model.PriorityName = email.PriorityId.GetTranslationEnum(_translationService, _workContext);
        model.CreatedOn = _dateTimeService.ConvertToUserTime(email.CreatedOnUtc, DateTimeKind.Utc);
        if (email.SentOnUtc.HasValue)
            model.SentOn = _dateTimeService.ConvertToUserTime(email.SentOnUtc.Value, DateTimeKind.Utc);
        if (email.ReadOnUtc.HasValue)
            model.ReadOn = _dateTimeService.ConvertToUserTime(email.ReadOnUtc.Value, DateTimeKind.Utc);
        if (email.DontSendBeforeDateUtc.HasValue)
            model.DontSendBeforeDate = email.DontSendBeforeDateUtc.ConvertToUserTime(_dateTimeService);
        else model.SendImmediately = true;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> Requeue(QueuedEmailModel queuedEmailModel)
    {
        var queuedEmail = await _queuedEmailService.GetQueuedEmailById(queuedEmailModel.Id);
        if (queuedEmail == null)
            //No email found with the specified id
            return RedirectToAction("List");

        var requestedEmail = new QueuedEmail {
            PriorityId = queuedEmail.PriorityId,
            From = queuedEmail.From,
            FromName = queuedEmail.FromName,
            To = queuedEmail.To,
            ToName = queuedEmail.ToName,
            ReplyTo = queuedEmail.ReplyTo,
            ReplyToName = queuedEmail.ReplyToName,
            CC = queuedEmail.CC,
            Bcc = queuedEmail.Bcc,
            Subject = queuedEmail.Subject,
            Body = queuedEmail.Body,
            AttachmentFilePath = queuedEmail.AttachmentFilePath,
            AttachmentFileName = queuedEmail.AttachmentFileName,
            AttachedDownloads = queuedEmail.AttachedDownloads,
            EmailAccountId = queuedEmail.EmailAccountId,
            DontSendBeforeDateUtc = queuedEmailModel.SendImmediately || !queuedEmailModel.DontSendBeforeDate.HasValue
                ? null
                : _dateTimeService.ConvertToUtcTime(queuedEmailModel.DontSendBeforeDate.Value)
        };
        await _queuedEmailService.InsertQueuedEmail(requestedEmail);

        Success(_translationService.GetResource("Admin.System.QueuedEmails.Requeued"));
        return RedirectToAction("Edit", new { id = requestedEmail.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var email = await _queuedEmailService.GetQueuedEmailById(id);
        if (email == null)
            //No email found with the specified id
            return RedirectToAction("List");

        await _queuedEmailService.DeleteQueuedEmail(email);

        Success(_translationService.GetResource("Admin.System.QueuedEmails.Deleted"));
        return RedirectToAction("List");
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> DeleteSelected(ICollection<string> selectedIds)
    {
        if (selectedIds != null)
        {
            var queuedEmails = await _queuedEmailService.GetQueuedEmailsByIds(selectedIds.ToArray());
            foreach (var queuedEmail in queuedEmails)
                await _queuedEmailService.DeleteQueuedEmail(queuedEmail);
        }

        return Json(new { Result = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> DeleteAll()
    {
        await _queuedEmailService.DeleteAllEmails();
        Success(_translationService.GetResource("Admin.System.QueuedEmails.DeletedAll"));
        return RedirectToAction("List");
    }
}