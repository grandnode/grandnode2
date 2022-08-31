using Grand.Business.Core.Interfaces.System.Installation;
using Grand.Domain.Pages;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallPageLayouts()
        {
            var pageLayouts = new List<PageLayout>
                               {
                                   new PageLayout
                                       {
                                           Name = "Default layout",
                                           ViewPath = "PageDetails",
                                           DisplayOrder = 1
                                       },
                               };
            await _pageLayoutRepository.InsertAsync(pageLayouts);
        }
    }
}
