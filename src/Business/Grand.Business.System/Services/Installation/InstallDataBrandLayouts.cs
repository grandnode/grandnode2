using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Catalog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
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
                                       },
                               };
            await _brandLayoutRepository.InsertAsync(brandLayouts);
        }
    }
}
