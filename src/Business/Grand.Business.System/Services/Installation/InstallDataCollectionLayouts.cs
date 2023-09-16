using Grand.Domain.Catalog;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService
    {
        protected virtual async Task InstallCollectionLayouts()
        {
            var collectionLayouts = new List<CollectionLayout>
                               {
                                   new CollectionLayout
                                       {
                                           Name = "Grid or Lines",
                                           ViewPath = "CollectionLayout.GridOrLines",
                                           DisplayOrder = 1
                                       }
                               };
            await _collectionLayoutRepository.InsertAsync(collectionLayouts);
        }
    }
}
