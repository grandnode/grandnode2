using Grand.Business.Core.Interfaces.System.Installation;
using Grand.Domain.Catalog;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallCategoryLayouts()
        {
            // Grid category layout
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

            // Grid category layout
            var categoryLayoutsCard = new List<CategoryLayout>
                               {
                                   new CategoryLayout
                                       {
                                           Name = "Grid or Lines Card",
                                           ViewPath = "CategoryLayout.GridOrLines_Card",
                                           DisplayOrder = 1
                                       },
                               };
            await _categoryLayoutRepository.InsertAsync(categoryLayoutsCard);
        }
    }
}
