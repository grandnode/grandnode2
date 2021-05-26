using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Messages.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Messages.Services
{
    public partial class MessageTemplateService : IMessageTemplateService
    {
        #region Fields

        private readonly IRepository<MessageTemplate> _messageTemplateRepository;
        private readonly IAclService _aclService;
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        public MessageTemplateService(ICacheBase cacheBase,
            IAclService aclService,
            IRepository<MessageTemplate> messageTemplateRepository,
            IMediator mediator)
        {
            _cacheBase = cacheBase;
            _aclService = aclService;
            _messageTemplateRepository = messageTemplateRepository;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Inserts a message template
        /// </summary>
        /// <param name="messageTemplate">Message template</param>
        public virtual async Task InsertMessageTemplate(MessageTemplate messageTemplate)
        {
            if (messageTemplate == null)
                throw new ArgumentNullException(nameof(messageTemplate));

            await _messageTemplateRepository.InsertAsync(messageTemplate);

            await _cacheBase.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(messageTemplate);
        }

        /// <summary>
        /// Updates a message template
        /// </summary>
        /// <param name="messageTemplate">Message template</param>
        public virtual async Task UpdateMessageTemplate(MessageTemplate messageTemplate)
        {
            if (messageTemplate == null)
                throw new ArgumentNullException(nameof(messageTemplate));

            await _messageTemplateRepository.UpdateAsync(messageTemplate);

            await _cacheBase.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(messageTemplate);
        }

        /// <summary>
        /// Delete a message template
        /// </summary>
        /// <param name="messageTemplate">Message template</param>
        public virtual async Task DeleteMessageTemplate(MessageTemplate messageTemplate)
        {
            if (messageTemplate == null)
                throw new ArgumentNullException(nameof(messageTemplate));

            await _messageTemplateRepository.DeleteAsync(messageTemplate);

            await _cacheBase.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(messageTemplate);
        }

        /// <summary>
        /// Gets a message template
        /// </summary>
        /// <param name="messageTemplateId">Message template identifier</param>
        /// <returns>Message template</returns>
        public virtual Task<MessageTemplate> GetMessageTemplateById(string messageTemplateId)
        {
            return _messageTemplateRepository.GetByIdAsync(messageTemplateId);
        }

        /// <summary>
        /// Gets a message template
        /// </summary>
        /// <param name="messageTemplateName">Message template name</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Message template</returns>
        public virtual async Task<MessageTemplate> GetMessageTemplateByName(string messageTemplateName, string storeId)
        {
            if (string.IsNullOrWhiteSpace(messageTemplateName))
                throw new ArgumentException("messageTemplateName");

            string key = string.Format(CacheKey.MESSAGETEMPLATES_BY_NAME_KEY, messageTemplateName, storeId);
            return await _cacheBase.GetAsync(key, async () =>
            {
                var query = from p in _messageTemplateRepository.Table
                            select p;

                query = query.Where(t => t.Name == messageTemplateName);
                query = query.OrderBy(t => t.Id);
                var templates = await Task.FromResult(query.ToList());

                //store acl
                if (!String.IsNullOrEmpty(storeId))
                {
                    templates = templates
                        .Where(t => _aclService.Authorize(t, storeId))
                        .ToList();
                }

                return templates.FirstOrDefault();
            });

        }

        /// <summary>
        /// Gets all message templates
        /// </summary>
        /// <param name="storeId">Store identifier; pass "" to load all records</param>
        /// <returns>Message template list</returns>
        public virtual async Task<IList<MessageTemplate>> GetAllMessageTemplates(string storeId)
        {
            string key = string.Format(CacheKey.MESSAGETEMPLATES_ALL_KEY, storeId);
            return await _cacheBase.GetAsync(key, async () =>
            {
                var query = from p in _messageTemplateRepository.Table
                            select p;

                query = query.OrderBy(t => t.Name);

                //Store acl
                if (!String.IsNullOrEmpty(storeId) && !CommonHelper.IgnoreStoreLimitations)
                {
                    query = from p in query
                            where !p.LimitedToStores || p.Stores.Contains(storeId)
                            select p;
                    query = query.OrderBy(t => t.Name);
                }
                return await Task.FromResult(query.ToList());
            });
        }

        /// <summary>
        /// Create a copy of message template with all depended data
        /// </summary>
        /// <param name="messageTemplate">Message template</param>
        /// <returns>Message template copy</returns>
        public virtual async Task<MessageTemplate> CopyMessageTemplate(MessageTemplate messageTemplate)
        {
            if (messageTemplate == null)
                throw new ArgumentNullException(nameof(messageTemplate));

            var mtCopy = new MessageTemplate
            {
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
}
