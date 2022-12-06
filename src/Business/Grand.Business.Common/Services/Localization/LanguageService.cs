using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.Domain.Data;
using Grand.Domain.Localization;
using MediatR;

namespace Grand.Business.Common.Services.Localization
{
    /// <summary>
    /// Language service
    /// </summary>
    public class LanguageService : ILanguageService
    {
        #region Fields

        private readonly IRepository<Language> _languageRepository;
        private readonly ICacheBase _cacheBase;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="cacheBase">Cache manager</param>
        /// <param name="languageRepository">Language repository</param>
        /// <param name="mediator">Mediator</param>
        public LanguageService(ICacheBase cacheBase,
            IRepository<Language> languageRepository,
            IMediator mediator)
        {
            _cacheBase = cacheBase;
            _languageRepository = languageRepository;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        
        /// <summary>
        /// Gets all languages
        /// </summary>
        /// <param name="storeId">Load records allowed only in a specified store; pass "" to load all records</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Languages</returns>
        public virtual async Task<IList<Language>> GetAllLanguages(bool showHidden = false, string storeId = "")
        {
            var key = string.Format(CacheKey.LANGUAGES_ALL_KEY, showHidden);
            var languages = await _cacheBase.GetAsync(key, async () =>
            {
                var query = from p in _languageRepository.Table
                            select p;

                if (!showHidden)
                    query = query.Where(l => l.Published);
                query = query.OrderBy(l => l.DisplayOrder);
                return await Task.FromResult(query.ToList());
            });

            //store acl
            if (!string.IsNullOrWhiteSpace(storeId))
            {
                languages = languages
                    .Where(l => l.Stores.Contains(storeId) || !l.LimitedToStores)
                    .ToList();
            }
            return languages;
        }

        /// <summary>
        /// Gets a language
        /// </summary>
        /// <param name="languageId">Language identifier</param>
        /// <returns>Language</returns>
        public virtual Task<Language> GetLanguageById(string languageId)
        {
            var key = string.Format(CacheKey.LANGUAGES_BY_ID_KEY, languageId);
            return _cacheBase.GetAsync(key, () => _languageRepository.GetByIdAsync(languageId));
        }

        /// <summary>
        /// Gets a language
        /// </summary>
        /// <param name="languageCode">Language code</param>
        /// <returns>Language</returns>
        public virtual async Task<Language> GetLanguageByCode(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
                throw new ArgumentNullException(nameof(languageCode));

            var key = string.Format(CacheKey.LANGUAGES_BY_CODE, languageCode);
            return await _cacheBase.GetAsync(key, async () =>
            {
                var query = from q in _languageRepository.Table
                            where q.UniqueSeoCode.ToLowerInvariant() == languageCode.ToLowerInvariant()
                            select q;
                return await Task.FromResult(query.FirstOrDefault());
            });

        }

        /// <summary>
        /// Inserts a language
        /// </summary>
        /// <param name="language">Language</param>
        public virtual async Task InsertLanguage(Language language)
        {
            if (language == null)
                throw new ArgumentNullException(nameof(language));

            await _languageRepository.InsertAsync(language);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.LANGUAGES_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(language);
        }

        /// <summary>
        /// Updates a language
        /// </summary>
        /// <param name="language">Language</param>
        public virtual async Task UpdateLanguage(Language language)
        {
            if (language == null)
                throw new ArgumentNullException(nameof(language));

            //update language
            await _languageRepository.UpdateAsync(language);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.LANGUAGES_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(language);
        }
        /// <summary>
        /// Deletes a language
        /// </summary>
        /// <param name="language">Language</param>
        public virtual async Task DeleteLanguage(Language language)
        {
            if (language == null)
                throw new ArgumentNullException(nameof(language));

            await _languageRepository.DeleteAsync(language);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.LANGUAGES_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(language);
        }

        #endregion
    }
}
