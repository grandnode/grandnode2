using Grand.Domain.Localization;

namespace Grand.Business.Core.Interfaces.Common.Localization
{
    /// <summary>
    /// Language service interface
    /// </summary>
    public interface ILanguageService
    {
        
        /// <summary>
        /// Gets all languages
        /// </summary>
        /// <param name="storeId">Load records allowed only in a specified store; pass "" to load all records</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Languages</returns>
        Task<IList<Language>> GetAllLanguages(bool showHidden = false, string storeId = "");

        /// <summary>
        /// Gets a language
        /// </summary>
        /// <param name="languageId">Language identifier</param>
        /// <returns>Language</returns>
        Task<Language> GetLanguageById(string languageId);

        /// <summary>
        /// Gets a language
        /// </summary>
        /// <param name="languageCode">Language code</param>
        /// <returns>Language</returns>
        Task<Language> GetLanguageByCode(string languageCode);

        /// <summary>
        /// Inserts a language
        /// </summary>
        /// <param name="language">Language</param>
        Task InsertLanguage(Language language);

        /// <summary>
        /// Updates a language
        /// </summary>
        /// <param name="language">Language</param>
        Task UpdateLanguage(Language language);

        /// <summary>
        /// Deletes a language
        /// </summary>
        /// <param name="language">Language</param>
        Task DeleteLanguage(Language language);

    }
}
