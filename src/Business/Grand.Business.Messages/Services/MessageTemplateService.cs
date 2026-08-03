using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Messages.Services;

public class MessageTemplateService : IMessageTemplateService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    public MessageTemplateService(ICacheBase cacheBase,
        IAclService aclService,
        IRepository<MessageTemplate> messageTemplateRepository,
        IMediator mediator, AccessControlConfig accessControlConfig)
    {
        _cacheBase = cacheBase;
        _aclService = aclService;
        _messageTemplateRepository = messageTemplateRepository;
        _mediator = mediator;
        _accessControlConfig = accessControlConfig;
    }

    #endregion

    #region Fields

    private readonly IRepository<MessageTemplate> _messageTemplateRepository;
    private readonly IAclService _aclService;
    private readonly IMediator _mediator;
    private readonly ICacheBase _cacheBase;
    private readonly AccessControlConfig _accessControlConfig;

    #endregion

    #region Methods

    /// <summary>
    ///     Inserts a message template
    /// </summary>
    /// <param name="messageTemplate">Message template</param>
    public virtual async Task InsertMessageTemplate(MessageTemplate messageTemplate)
    {
        ArgumentNullException.ThrowIfNull(messageTemplate);

        await _messageTemplateRepository.InsertAsync(messageTemplate);

        await _cacheBase.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(messageTemplate);
    }

    /// <summary>
    ///     Updates a message template
    /// </summary>
    /// <param name="messageTemplate">Message template</param>
    public virtual async Task UpdateMessageTemplate(MessageTemplate messageTemplate)
    {
        ArgumentNullException.ThrowIfNull(messageTemplate);

        await _messageTemplateRepository.UpdateAsync(messageTemplate);

        await _cacheBase.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(messageTemplate);
    }

    /// <summary>
    ///     Delete a message template
    /// </summary>
    /// <param name="messageTemplate">Message template</param>
    public virtual async Task DeleteMessageTemplate(MessageTemplate messageTemplate)
    {
        ArgumentNullException.ThrowIfNull(messageTemplate);

        await _messageTemplateRepository.DeleteAsync(messageTemplate);

        await _cacheBase.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(messageTemplate);
    }

    /// <summary>
    ///     Gets a message template
    /// </summary>
    /// <param name="messageTemplateId">Message template identifier</param>
    /// <returns>Message template</returns>
    public virtual Task<MessageTemplate> GetMessageTemplateById(string messageTemplateId)
    {
        return _messageTemplateRepository.GetByIdAsync(messageTemplateId);
    }

    /// <summary>
    ///     Gets a message template
    /// </summary>
    /// <param name="messageTemplateName">Message template name</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>Message template</returns>
    public virtual async Task<MessageTemplate> GetMessageTemplateByName(string messageTemplateName, string storeId)
    {
        if (string.IsNullOrWhiteSpace(messageTemplateName))
            throw new ArgumentException(null, nameof(messageTemplateName));

        var key = string.Format(CacheKey.MESSAGETEMPLATES_BY_NAME_KEY, messageTemplateName, storeId);
        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _messageTemplateRepository.Table
                select p;

            query = query.Where(t => t.Name == messageTemplateName);
            query = query.OrderBy(t => t.Id);
            var templates = await Task.FromResult(query.ToList());

            //store acl
            if (!string.IsNullOrEmpty(storeId))
                templates = templates
                    .Where(t => _aclService.Authorize(t, storeId))
                    .ToList();

            return templates.FirstOrDefault();
        });
    }

    /// <summary>
    ///     Gets all message templates
    /// </summary>
    /// <param name="storeId">Store identifier; pass "" to load all records</param>
    /// <param name="keywords">Keywords to filter by name or subject; pass "" to skip</param>
    /// <param name="pageIndex">Page index (0-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paged list of message templates</returns>
    public virtual async Task<IPagedList<MessageTemplate>> GetAllMessageTemplates(string storeId, string keywords = "", int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = from p in _messageTemplateRepository.Table
            select p;

        //Store acl
        if (!string.IsNullOrEmpty(storeId) && !_accessControlConfig.IgnoreStoreLimitations)
            query = from p in query
                where !p.LimitedToStores || p.Stores.Contains(storeId)
                select p;

        if (!string.IsNullOrEmpty(keywords))
        {
            var lowerKeywords = keywords.ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(lowerKeywords) ||
                                     (t.Subject != null && t.Subject.Contains(lowerKeywords, StringComparison.CurrentCultureIgnoreCase)));
        }

        query = query.OrderBy(t => t.Name);

        return await PagedList<MessageTemplate>.Create(query, pageIndex, pageSize);
    }

    /// <summary>
    ///     Create a copy of message template with all depended data
    /// </summary>
    /// <param name="messageTemplate">Message template</param>
    /// <returns>Message template copy</returns>
    public virtual async Task<MessageTemplate> CopyMessageTemplate(MessageTemplate messageTemplate)
    {
        ArgumentNullException.ThrowIfNull(messageTemplate);

        var mtCopy = new MessageTemplate {
            Name = messageTemplate.Name,
            BccEmailAddresses = messageTemplate.BccEmailAddresses,
            Subject = messageTemplate.Subject,
            Body = messageTemplate.Body,
            IsActive = messageTemplate.IsActive,
            AttachedDownloadId = messageTemplate.AttachedDownloadId,
            EmailAccountId = messageTemplate.EmailAccountId,
            LimitedToStores = messageTemplate.LimitedToStores,
            Locales = messageTemplate.Locales,
            Stores = messageTemplate.Stores,
            DelayBeforeSend = messageTemplate.DelayBeforeSend,
            DelayPeriodId = messageTemplate.DelayPeriodId
        };

        await InsertMessageTemplate(mtCopy);

        return mtCopy;
    }

    #endregion
}