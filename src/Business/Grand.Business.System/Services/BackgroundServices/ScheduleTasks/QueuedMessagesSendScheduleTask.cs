using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Microsoft.Extensions.Logging;

namespace Grand.Business.System.Services.BackgroundServices.ScheduleTasks;

/// <summary>
///     Represents a task for sending queued message
/// </summary>
public class QueuedMessagesSendScheduleTask : IScheduleTask
{
    private readonly IEmailAccountService _emailAccountService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<QueuedMessagesSendScheduleTask> _logger;
    private readonly IQueuedEmailService _queuedEmailService;


    public QueuedMessagesSendScheduleTask(IQueuedEmailService queuedEmailService,
        IEmailSender emailSender, ILogger<QueuedMessagesSendScheduleTask> logger,
        IEmailAccountService emailAccountService)
    {
        _queuedEmailService = queuedEmailService;
        _emailSender = emailSender;
        _logger = logger;
        _emailAccountService = emailAccountService;
    }

    /// <summary>
    ///     Executes a task
    /// </summary>
    public async Task Execute()
    {
        const int maxTries = 3;
        var queuedEmails = await _queuedEmailService.SearchEmails(null, null, null, null, null, true, true, maxTries,
            false, -1, null, 0, 500);
        foreach (var queuedEmail in queuedEmails)
        {
            var bcc = string.IsNullOrWhiteSpace(queuedEmail.Bcc)
                ? null
                : queuedEmail.Bcc.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var cc = string.IsNullOrWhiteSpace(queuedEmail.CC)
                ? null
                : queuedEmail.CC.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                var emailAccount = await _emailAccountService.GetEmailAccountById(queuedEmail.EmailAccountId);
                await _emailSender.SendEmail(emailAccount,
                    queuedEmail.Subject,
                    queuedEmail.Body,
                    queuedEmail.From,
                    queuedEmail.FromName,
                    queuedEmail.To,
                    queuedEmail.ToName,
                    queuedEmail.ReplyTo,
                    queuedEmail.ReplyToName,
                    bcc,
                    cc,
                    queuedEmail.AttachmentFilePath,
                    queuedEmail.AttachmentFileName,
                    queuedEmail.AttachedDownloads);

                queuedEmail.SentOnUtc = DateTime.UtcNow;
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "Error sending e-mail. {ExcMessage}", exc.Message);
            }
            finally
            {
                queuedEmail.SentTries = queuedEmail.SentTries + 1;
                await _queuedEmailService.UpdateQueuedEmail(queuedEmail);
            }
        }
    }
}