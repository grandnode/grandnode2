using Grand.Business.Messages.Interfaces;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.Domain.Data;
using Grand.Domain.Messages;
using Grand.SharedKernel;
using Grand.SharedKernel.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Messages.Services
{
    public partial class EmailAccountService : IEmailAccountService
    {
        private readonly IRepository<EmailAccount> _emailAccountRepository;
        private readonly ICacheBase _cacheBase;
        private readonly IMediator _mediator;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="emailAccountRepository">Email account repository</param>
        /// <param name="cacheBase">Cache manager</param>
        /// <param name="mediator">Mediator</param>
        public EmailAccountService(
            IRepository<EmailAccount> emailAccountRepository,
            ICacheBase cacheBase,
            IMediator mediator)
        {
            _emailAccountRepository = emailAccountRepository;
            _cacheBase = cacheBase;
            _mediator = mediator;
        }

        /// <summary>
        /// Inserts an email account
        /// </summary>
        /// <param name="emailAccount">Email account</param>
        public virtual async Task InsertEmailAccount(EmailAccount emailAccount)
        {
            if (emailAccount == null)
                throw new ArgumentNullException(nameof(emailAccount));

            emailAccount.Email = CommonHelper.EnsureNotNull(emailAccount.Email);
            emailAccount.DisplayName = CommonHelper.EnsureNotNull(emailAccount.DisplayName);
            emailAccount.Host = CommonHelper.EnsureNotNull(emailAccount.Host);
            emailAccount.Username = CommonHelper.EnsureNotNull(emailAccount.Username);
            emailAccount.Password = CommonHelper.EnsureNotNull(emailAccount.Password);

            emailAccount.Email = emailAccount.Email.Trim();
            emailAccount.DisplayName = emailAccount.DisplayName.Trim();
            emailAccount.Host = emailAccount.Host.Trim();
            emailAccount.Username = emailAccount.Username.Trim();
            emailAccount.Password = emailAccount.Password.Trim();

            emailAccount.Email = CommonHelper.EnsureMaximumLength(emailAccount.Email, 255);
            emailAccount.DisplayName = CommonHelper.EnsureMaximumLength(emailAccount.DisplayName, 255);
            emailAccount.Host = CommonHelper.EnsureMaximumLength(emailAccount.Host, 255);
            emailAccount.Username = CommonHelper.EnsureMaximumLength(emailAccount.Username, 255);
            emailAccount.Password = CommonHelper.EnsureMaximumLength(emailAccount.Password, 255);

            await _emailAccountRepository.InsertAsync(emailAccount);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.EMAILACCOUNT_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(emailAccount);
        }

        /// <summary>
        /// Updates an email account
        /// </summary>
        /// <param name="emailAccount">Email account</param>
        public virtual async Task UpdateEmailAccount(EmailAccount emailAccount)
        {
            if (emailAccount == null)
                throw new ArgumentNullException(nameof(emailAccount));

            emailAccount.Email = CommonHelper.EnsureNotNull(emailAccount.Email);
            emailAccount.DisplayName = CommonHelper.EnsureNotNull(emailAccount.DisplayName);
            emailAccount.Host = CommonHelper.EnsureNotNull(emailAccount.Host);
            emailAccount.Username = CommonHelper.EnsureNotNull(emailAccount.Username);
            emailAccount.Password = CommonHelper.EnsureNotNull(emailAccount.Password);

            emailAccount.Email = emailAccount.Email.Trim();
            emailAccount.DisplayName = emailAccount.DisplayName.Trim();
            emailAccount.Host = emailAccount.Host.Trim();
            emailAccount.Username = emailAccount.Username.Trim();
            emailAccount.Password = emailAccount.Password.Trim();

            emailAccount.Email = CommonHelper.EnsureMaximumLength(emailAccount.Email, 255);
            emailAccount.DisplayName = CommonHelper.EnsureMaximumLength(emailAccount.DisplayName, 255);
            emailAccount.Host = CommonHelper.EnsureMaximumLength(emailAccount.Host, 255);
            emailAccount.Username = CommonHelper.EnsureMaximumLength(emailAccount.Username, 255);
            emailAccount.Password = CommonHelper.EnsureMaximumLength(emailAccount.Password, 255);

            await _emailAccountRepository.UpdateAsync(emailAccount);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.EMAILACCOUNT_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(emailAccount);
        }

        /// <summary>
        /// Deletes an email account
        /// </summary>
        /// <param name="emailAccount">Email account</param>
        public virtual async Task DeleteEmailAccount(EmailAccount emailAccount)
        {
            if (emailAccount == null)
                throw new ArgumentNullException(nameof(emailAccount));
            var emailAccounts = await GetAllEmailAccounts();
            if (emailAccounts.Count == 1)
                throw new GrandException("You cannot delete this email account. At least one account is required.");

            await _emailAccountRepository.DeleteAsync(emailAccount);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.EMAILACCOUNT_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(emailAccount);
        }

        /// <summary>
        /// Gets an email account by identifier
        /// </summary>
        /// <param name="emailAccountId">The email account identifier</param>
        /// <returns>Email account</returns>
        public virtual async Task<EmailAccount> GetEmailAccountById(string emailAccountId)
        {
            string key = string.Format(CacheKey.EMAILACCOUNT_BY_ID_KEY, emailAccountId);
            return await _cacheBase.GetAsync(key, () =>
            {
                return _emailAccountRepository.GetByIdAsync(emailAccountId);
            });

        }

        /// <summary>
        /// Gets all email accounts
        /// </summary>
        /// <returns>Email accounts list</returns>
        public virtual async Task<IList<EmailAccount>> GetAllEmailAccounts()
        {
            return await _cacheBase.GetAsync(CacheKey.EMAILACCOUNT_ALL_KEY, async () =>
            {
                var query = from ea in _emailAccountRepository.Table
                            select ea;
                return await Task.FromResult(query.ToList());
            });
        }
    }
}
