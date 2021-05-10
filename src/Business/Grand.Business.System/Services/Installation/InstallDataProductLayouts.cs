using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Catalog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallProductLayouts()
        {
            var productLayouts = new List<ProductLayout>
                               {
                                   new ProductLayout
                                       {
                                           Name = "Simple product",
                                           ViewPath = "ProductLayout.Simple",
                                           DisplayOrder = 10
                                       },
                                   new ProductLayout
                                       {
                                           Name = "Grouped product (with variants)",
                                           ViewPath = "ProductLayout.Grouped",
                                           DisplayOrder = 100
                                       },
                               };
            await _productLayoutRepository.InsertAsync(productLayouts);
        }
    }
}
