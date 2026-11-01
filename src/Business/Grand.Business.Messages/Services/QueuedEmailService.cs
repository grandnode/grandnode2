using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Domain.Messages;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Messages.Services;

public class QueuedEmailService : IQueuedEmailService
{
    private readonly IDownloadService _downloadService;
    private readonly ILogger<QueuedEmailService> _logger;
    private readonly IMediator _mediator;
    private readonly IRepository<QueuedEmail> _queuedEmailRepository;

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="queuedEmailRepository">Queued email repository</param>
    /// <param name="downloadService">Download service</param>
    /// <param name="mediator">Mediator</param>
    /// <param name="logger">Logger</param>
    public QueuedEmailService(IRepository<QueuedEmail> queuedEmailRepository,
        IDownloadService downloadService,
        IMediator mediator,
        ILogger<QueuedEmailService> logger)
    {
        _queuedEmailRepository = queuedEmailRepository;
        _downloadService = downloadService;
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    ///     Inserts a queued email
    /// </summary>
    /// <param name="queuedEmail">Queued email</param>
    public virtual async Task InsertQueuedEmail(QueuedEmail queuedEmail)
    {
        ArgumentNullException.ThrowIfNull(queuedEmail);

        try
        {
            await _queuedEmailRepository.InsertAsync(queuedEmail);
        }
        catch
        {
            //the email was not stored, so nothing would ever delete its invoice
            await DeleteInvoices([queuedEmail.InvoiceDownloadId]);
            throw;
        }

        //event notification
        await _mediator.EntityInserted(queuedEmail);
    }

    /// <summary>
    ///     Updates a queued email
    /// </summary>
    /// <param name="queuedEmail">Queued email</param>
    public virtual async Task UpdateQueuedEmail(QueuedEmail queuedEmail)
    {
        ArgumentNullException.ThrowIfNull(queuedEmail);

        await _queuedEmailRepository.UpdateAsync(queuedEmail);

        //event notification
        await _mediator.EntityUpdated(queuedEmail);
    }

    /// <summary>
    ///     Deleted a queued email and its invoice
    /// </summary>
    /// <param name="queuedEmail">Queued email</param>
    public virtual async Task DeleteQueuedEmail(QueuedEmail queuedEmail)
    {
        ArgumentNullException.ThrowIfNull(queuedEmail);

        await DeleteInvoices([queuedEmail.InvoiceDownloadId]);

        await _queuedEmailRepository.DeleteAsync(queuedEmail);

        //event notification
        await _mediator.EntityDeleted(queuedEmail);
    }

    /// <summary>
    ///     Copies the invoice of a queued email, so that an email created from it owns a download of its own
    /// </summary>
    /// <param name="invoiceDownloadId">Download identifier of the invoice</param>
    /// <returns>Download identifier of the copy; null when there is no invoice to copy</returns>
    public virtual async Task<string> CopyInvoiceDownload(string invoiceDownloadId)
    {
        var invoice = await _downloadService.GetDownloadById(invoiceDownloadId);
        if (invoice == null)
            return null;

        var copy = new Download {
            Filename = invoice.Filename,
            Extension = invoice.Extension,
            ContentType = invoice.ContentType,
            DownloadBinary = invoice.DownloadBinary
        };
        await _downloadService.InsertDownload(copy);

        return copy.Id;
    }

    /// <summary>
    ///     Deleted a customer emails and their invoices
    /// </summary>
    /// <param name="email">email</param>
    public virtual async Task DeleteCustomerEmail(string email)
    {
        ArgumentNullException.ThrowIfNull(email);

        await DeleteInvoiceEmails(_queuedEmailRepository.Table.Where(x => x.To == email));

        await _queuedEmailRepository.DeleteManyAsync(x => x.To == email && x.InvoiceDownloadId == null);
    }

    /// <summary>
    ///     Deleted the invoice emails of orders and their invoices
    /// </summary>
    /// <param name="orderIds">Order identifiers</param>
    public virtual async Task DeleteOrderInvoiceEmails(IEnumerable<string> orderIds)
    {
        ArgumentNullException.ThrowIfNull(orderIds);

        var ids = orderIds.ToList();
        if (ids.Count == 0)
            return;

        await DeleteInvoiceEmails(_queuedEmailRepository.Table.Where(x =>
            x.Reference == Reference.Order && ids.Contains(x.ObjectId)));
    }

    /// <summary>
    ///     Gets a queued email by identifier
    /// </summary>
    /// <param name="queuedEmailId">Queued email identifier</param>
    /// <returns>Queued email</returns>
    public virtual Task<QueuedEmail> GetQueuedEmailById(string queuedEmailId)
    {
        return _queuedEmailRepository.GetByIdAsync(queuedEmailId);
    }

    /// <summary>
    ///     Get queued emails by identifiers
    /// </summary>
    /// <param name="queuedEmailIds">queued email identifiers</param>
    /// <returns>Queued emails</returns>
    public virtual async Task<IList<QueuedEmail>> GetQueuedEmailsByIds(string[] queuedEmailIds)
    {
        if (queuedEmailIds == null || queuedEmailIds.Length == 0)
            return new List<QueuedEmail>();

        var query = from qe in _queuedEmailRepository.Table
            where queuedEmailIds.Contains(qe.Id)
            select qe;
        var queuedEmails = query.ToList();
        //sort by passed identifiers
        var sortedQueuedEmails =
            queuedEmailIds.Select(id => queuedEmails.FirstOrDefault(email => email.Id == id))
                .Where(queuedEmail => queuedEmail != null)
                .ToList();
        
        return await Task.FromResult(sortedQueuedEmails);
    }

    /// <summary>
    ///     Gets all queued emails
    /// </summary>
    /// <param name="fromEmail">From Email</param>
    /// <param name="toEmail">To Email</param>
    /// <param name="text">Text</param>
    /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
    /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>
    /// <param name="loadNotSentItemsOnly">A value indicating whether to load only not sent emails</param>
    /// <param name="maxSendTries">Maximum send tries</param>
    /// <param name="loadOnlyItemsToBeSent">Load only items to be sent</param>
    /// <param name="loadNewest">A value indicating whether we should sort queued email descending; otherwise, ascending.</param>
    /// <param name="referenceId">Reference object</param>
    /// <param name="objectId">Object reference id</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Email item list</returns>
    public virtual async Task<IPagedList<QueuedEmail>> SearchEmails(string fromEmail,
        string toEmail, string text, DateTime? createdFromUtc, DateTime? createdToUtc,
        bool loadNotSentItemsOnly, bool loadOnlyItemsToBeSent, int maxSendTries,
        bool loadNewest, int referenceId = -1, string objectId = "", int pageIndex = 0, int pageSize = int.MaxValue)
    {
        fromEmail = (fromEmail ?? string.Empty).Trim();
        toEmail = (toEmail ?? string.Empty).Trim();

        var query = from p in _queuedEmailRepository.Table
            select p;

        if (!string.IsNullOrEmpty(fromEmail))
            query = query.Where(qe => qe.From.ToLower().Contains(fromEmail.ToLower()));
        if (!string.IsNullOrEmpty(toEmail))
            query = query.Where(qe => qe.To.ToLower().Contains(toEmail.ToLower()));
        if (!string.IsNullOrEmpty(text))
            query = query.Where(qe =>
                qe.Subject.ToLower().Contains(text.ToLower()) || qe.Body.ToLower().Contains(text.ToLower()));

        if (createdFromUtc.HasValue)
            query = query.Where(qe => qe.CreatedOnUtc >= createdFromUtc.Value);
        if (createdToUtc.HasValue)
            query = query.Where(qe => qe.CreatedOnUtc <= createdToUtc.Value);
        if (loadNotSentItemsOnly)
            query = query.Where(qe => !qe.SentOnUtc.HasValue);

        if (loadOnlyItemsToBeSent)
        {
            var nowUtc = DateTime.UtcNow;
            query = query.Where(qe => !qe.DontSendBeforeDateUtc.HasValue || qe.DontSendBeforeDateUtc.Value <= nowUtc);
        }

        if (referenceId >= 0)
            query = query.Where(qe => qe.Reference == (Reference)referenceId && qe.ObjectId == objectId);
        if (maxSendTries > 0)
            query = query.Where(qe => qe.SentTries < maxSendTries);

        //load the newest records
        query = loadNewest
            ? query.OrderByDescending(qe => qe.CreatedOnUtc)
            :
            //load by priority
            query.OrderByDescending(qe => qe.PriorityId).ThenBy(qe => qe.CreatedOnUtc);
        return await _queuedEmailRepository.PagedAsync(query, pageIndex, pageSize);
    }

    /// <summary>
    ///     Delete all queued emails and their invoices
    /// </summary>
    public virtual async Task DeleteAllEmails()
    {
        await DeleteInvoiceEmails(_queuedEmailRepository.Table);

        await _queuedEmailRepository.DeleteManyAsync(x => x.InvoiceDownloadId == null);
    }

    /// <summary>
    ///     Deletes the emails with an invoice, invoices first: once an email is gone, nothing leads to its invoice.
    ///     Only the emails read here are deleted, so one queued in the meantime keeps its invoice
    /// </summary>
    private async Task DeleteInvoiceEmails(IQueryable<QueuedEmail> emails)
    {
        var invoiceEmails = await _queuedEmailRepository.ToListAsync(emails
            .Where(x => x.InvoiceDownloadId != null)
            .Select(x => new QueuedEmail { Id = x.Id, InvoiceDownloadId = x.InvoiceDownloadId }));

        await DeleteInvoices(invoiceEmails.Select(x => x.InvoiceDownloadId));

        await _queuedEmailRepository.DeleteAsync(invoiceEmails);
    }

    private async Task DeleteInvoices(IEnumerable<string> invoiceDownloadIds)
    {
        foreach (var invoiceDownloadId in invoiceDownloadIds)
            try
            {
                await _downloadService.DeleteDownloadById(invoiceDownloadId);
            }
            catch (Exception ex)
            {
                //a broken file must not stop the rest nor the deletion of an email or an account
                _logger.LogError(ex, "Error deleting the invoice download {InvoiceDownloadId}", invoiceDownloadId);
            }
    }
}