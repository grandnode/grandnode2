using Grand.Domain.Pages;
using Grand.Web.Admin.Models.Pages;

namespace Grand.Web.Admin.Interfaces
{
    public interface IPageViewModelService
    {
        Task<PageListModel> PreparePageListModel();
        Task PrepareLayoutsModel(PageModel model);
        Task<Page> InsertPageModel(PageModel model);
        Task<Page> UpdatePageModel(Page page, PageModel model);
        Task DeletePage(Page page);
    }
}
