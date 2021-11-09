﻿using Grand.Business.Messages.Interfaces;
using Grand.Infrastructure.Extensions;
using Grand.Domain;
using Grand.Domain.Data;
using Grand.Domain.Messages;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grand.Domain.Common;

namespace Grand.Business.Messages.Services
{
    public partial class QueuedEmailService : IQueuedEmailService
    {
        private readonly IRepository<QueuedEmail> _queuedEmailRepository;
        private readonly IMediator _mediator;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="queuedEmailRepository">Queued email repository</param>
        /// <param name="mediator">Mediator</param>
        public QueuedEmailService(IRepository<QueuedEmail> queuedEmailRepository,
            IMediator mediator)
        {
            _queuedEmailRepository = queuedEmailRepository;
            _mediator = mediator;
        }

        /// <summary>
        /// Inserts a queued email
        /// </summary>
        /// <param name="queuedEmail">Queued email</param>        
        public virtual async Task InsertQueuedEmail(QueuedEmail queuedEmail)
        {
            if (queuedEmail == null)
                throw new ArgumentNullException(nameof(queuedEmail));

            await _queuedEmailRepository.InsertAsync(queuedEmail);

            //event notification
            await _mediator.EntityInserted(queuedEmail);
        }

        /// <summary>
        /// Updates a queued email
        /// </summary>
        /// <param name="queuedEmail">Queued email</param>
        public virtual async Task UpdateQueuedEmail(QueuedEmail queuedEmail)
        {
            if (queuedEmail == null)
                throw new ArgumentNullException(nameof(queuedEmail));

            await _queuedEmailRepository.UpdateAsync(queuedEmail);

            //event notification
            await _mediator.EntityUpdated(queuedEmail);
        }

        /// <summary>
        /// Deleted a queued email
        /// </summary>
        /// <param name="queuedEmail">Queued email</param>
        public virtual async Task DeleteQueuedEmail(QueuedEmail queuedEmail)
        {
            if (queuedEmail == null)
                throw new ArgumentNullException(nameof(queuedEmail));

            await _queuedEmailRepository.DeleteAsync(queuedEmail);

            //event notification
            await _mediator.EntityDeleted(queuedEmail);
        }

        /// <summary>
        /// Deleted a customer emails
        /// </summary>
        /// <param name="email">email</param>
        public virtual async Task DeleteCustomerEmail(string email)
        {
            if (email == null)
                throw new ArgumentNullException("email");

            var deleteCustomerEmail = _queuedEmailRepository.Table.Where(x => x.To == email);

            await _queuedEmailRepository.DeleteAsync(deleteCustomerEmail);
        }

        /// <summary>
        /// Gets a queued email by identifier
        /// </summary>
        /// <param name="queuedEmailId">Queued email identifier</param>
        /// <returns>Queued email</returns>
        public virtual Task<QueuedEmail> GetQueuedEmailById(string queuedEmailId)
        {
            return _queuedEmailRepository.GetByIdAsync(queuedEmailId);

        }

        /// <summary>
        /// Get queued emails by identifiers
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
            var sortedQueuedEmails = new List<QueuedEmail>();
            foreach (string id in queuedEmailIds)
            {
                var queuedEmail = queuedEmails.Find(x => x.Id == id);
                if (queuedEmail != null)
                    sortedQueuedEmails.Add(queuedEmail);
            }
            return await Task.FromResult(sortedQueuedEmails);
        }

        /// <summary>
        /// Gets all queued emails
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
                query = query.Where(qe => qe.Subject.ToLower().Contains(text.ToLower()) || qe.Body.ToLower().Contains(text.ToLower()));

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
            {
                query = query.Where(qe => qe.Reference == (Reference)referenceId && qe.ObjectId == objectId);
            }
            if (maxSendTries > 0)
                query = query.Where(qe => qe.SentTries < maxSendTries);

            if (loadNewest)
            {
                //load the newest records
                query = query.OrderByDescending(qe => qe.CreatedOnUtc);
            }
            else
            {
                //load by priority
                query = query.OrderByDescending(qe => qe.PriorityId).ThenBy(qe => qe.CreatedOnUtc);
            }
            return await PagedList<QueuedEmail>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Delete all queued emails
        /// </summary>
        public virtual async Task DeleteAllEmails()
        {
            await _queuedEmailRepository.ClearAsync();
        }
    }
}
