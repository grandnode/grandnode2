using Grand.Domain.Catalog;

namespace Grand.Business.Core.Interfaces.Catalog.Collections
{
    public partial interface ICollectionLayoutService
    {

        /// <summary>
        /// Gets all existing collection layouts
        /// </summary>
        /// <returns>Collection layouts</returns>
        Task<IList<CollectionLayout>> GetAllCollectionLayouts();

        /// <summary>
        /// Gets an existing collection layout
        /// </summary>
        /// <param name="collectionLayoutId">Collection layout id</param>
        /// <returns>Collection layout</returns>
        Task<CollectionLayout> GetCollectionLayoutById(string collectionLayoutId);

        /// <summary>
        /// Inserts a new collection layout
        /// </summary>
        /// <param name="collectionLayout">Collection layout</param>
        Task InsertCollectionLayout(CollectionLayout collectionLayout);

        /// <summary>
        /// Updates the existing collection layout
        /// </summary>
        /// <param name="collectionLayout">Collection layout</param>
        Task UpdateCollectionLayout(CollectionLayout collectionLayout);
        /// <summary>
        /// Deletes existing collection layout
        /// </summary>
        /// <param name="collectionLayout">Collection layout</param>
        Task DeleteCollectionLayout(CollectionLayout collectionLayout);
    }
}
