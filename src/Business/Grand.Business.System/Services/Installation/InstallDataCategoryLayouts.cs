using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Catalog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallCategoryLayouts()
        {
            var categoryLayouts = new List<CategoryLayout>
                               {
                                   new CategoryLayout
                                       {
                                           Name = "Grid or Lines",
                                           ViewPath = "CategoryLayout.GridOrLines",
                                           DisplayOrder = 1
                                       },
                               };
            await _categoryLayoutRepository.InsertAsync(categoryLayouts);
        }
    }
}
