using Grand.Domain.Catalog;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService
    {
        protected virtual async Task InstallBrandLayouts()
        {
            var brandLayouts = new List<BrandLayout>
                               {
                                   new BrandLayout
                                       {
                                           Name = "Grid or Lines",
                                           ViewPath = "BrandLayout.GridOrLines",
                                           DisplayOrder = 1
                                       }
                               };
            await _brandLayoutRepository.InsertAsync(brandLayouts);
        }
    }
}
