using Grand.Domain.Catalog;

namespace Grand.Business.System.Services.Installation;

public partial class InstallationService
{
    protected virtual Task InstallCollectionLayouts()
    {
        var collectionLayouts = new List<CollectionLayout> {
            new() {
                Name = "Grid or Lines",
                ViewPath = "CollectionLayout.GridOrLines",
                DisplayOrder = 1
            }
        };
        collectionLayouts.ForEach(x => _collectionLayoutRepository.Insert(x));
        return Task.CompletedTask;
    }
}