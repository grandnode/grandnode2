using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Microsoft.Extensions.Logging;

namespace Grand.Module.ScheduledTasks.BackgroundServices;

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
    public async Task Execute(CancellationToken cancellationToken)
    {
        const int maxTries = 3;
        var queuedEmails = await _queuedEmailService.SearchEmails(null, null, null, null, null, true, true, maxTries,
            false, -1, null, 0, 500);
        foreach (var queuedEmail in queuedEmails)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bcc = string.IsNullOrWhiteSpace(queuedEmail.Bcc)
                ? null
                : queuedEmail.Bcc.Split([';'], StringSplitOptions.RemoveEmptyEntries);
            var cc = string.IsNullOrWhiteSpace(queuedEmail.CC)
                ? null
                : queuedEmail.CC.Split([';'], StringSplitOptions.RemoveEmptyEntries);

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
                    queuedEmail.AttachedDownloads,
                    cancellationToken);

                queuedEmail.SentOnUtc = DateTime.UtcNow;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                //the run was stopped mid-send - the e-mail stays queued for the next run
                //and the interrupted attempt does not count as a try
                throw;
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "Error sending e-mail. {ExcMessage}", exc.Message);
            }

            queuedEmail.SentTries = queuedEmail.SentTries + 1;
            await _queuedEmailService.UpdateQueuedEmail(queuedEmail);
        }
    }
}