using Grand.Domain.Catalog;

namespace Grand.Business.System.Services.Installation;

public partial class InstallationService
{
    protected virtual Task InstallBrandLayouts()
    {
        var brandLayouts = new List<BrandLayout> {
            new() {
                Name = "Grid or Lines",
                ViewPath = "BrandLayout.GridOrLines",
                DisplayOrder = 1
            }
        };
        brandLayouts.ForEach(x => _brandLayoutRepository.Insert(x));
        return Task.CompletedTask;
    }
}