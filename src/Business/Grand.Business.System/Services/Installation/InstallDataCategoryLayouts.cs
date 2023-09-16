using Grand.Domain.Catalog;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService
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
                                       }
                               };
            await _categoryLayoutRepository.InsertAsync(categoryLayouts);
        }
    }
}
