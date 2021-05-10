using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Catalog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
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
                                       },
                               };
            await _collectionLayoutRepository.InsertAsync(collectionLayouts);
        }
    }
}
