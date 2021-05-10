using Grand.Domain;
using Grand.Domain.Seo;
using System.Threading.Tasks;

namespace Grand.Business.Common.Interfaces.Seo
{
    /// <summary>
    /// Provides information about URL Entity
    /// </summary>
    public partial interface ISlugService
    {
       
        /// <summary>
        /// Gets an entity url
        /// </summary>
        /// <param name="entityUrlId">URL entity identifier</param>
        /// <returns>URL Entity</returns>
        Task<EntityUrl> GetEntityUrlById(string urlEntityId);

        /// <summary>
        /// Inserts an URL Entity
        /// </summary>
        /// <param name="entityUrl">URL Entity</param>
        Task InsertEntityUrl(EntityUrl urlEntity);

        /// <summary>
        /// Updates the URL Entity
        /// </summary>
        /// <param name="urlEntity">URL Entity</param>
        Task UpdateEntityUrl(EntityUrl urlEntity);

        /// <summary>
        /// Deletes an Entity url
        /// </summary>
        /// <param name="entityUrl">URL Entity</param>
        Task DeleteEntityUrl(EntityUrl entityUrl);

        /// <summary>
        /// Find URL Entity
        /// </summary>
        /// <param name="slug">Slug</param>
        /// <returns>Found URL Entity</returns>
        Task<EntityUrl> GetBySlug(string slug);

        /// <summary>
        /// Find URL Entity (cached version).
        /// </summary>
        /// <param name="slug">Slug</param>
        /// <returns>Found URL Entity</returns>
        Task<EntityUrl> GetBySlugCached(string slug);

        /// <summary>
        /// Gets all URL Entity
        /// </summary>
        /// <param name="slug">Slug</param>
        /// <param name="active">active</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>URL Entity</returns>
        Task<IPagedList<EntityUrl>> GetAllEntityUrl(string slug = "", bool? active = null, int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Find slug
        /// </summary>
        /// <param name="entityId">Entity identifier</param>
        /// <param name="entityName">Entity name</param>
        /// <param name="languageId">Language identifier</param>
        /// <returns>Found slug</returns>
        Task<string> GetActiveSlug(string entityId, string entityName, string languageId);

        /// <summary>
        /// Save slug
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="slug">Slug</param>
        /// <param name="languageId">Language ID</param>
        Task SaveSlug<T>(T entity, string slug, string languageId) where T : BaseEntity, ISlugEntity;
    }
}