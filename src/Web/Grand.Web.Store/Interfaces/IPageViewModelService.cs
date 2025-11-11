using Grand.Domain.Pages;
using Grand.Web.Store.Models.Pages;

namespace Grand.Web.Store.Interfaces;

public interface IPageViewModelService
{
    Task<PageListModel> PreparePageListModel();
    Task<PageModel> PreparePageModel(string storeId);
    Task<PageModel> PreparePageModel(Page page, string storeId);
    Task<Page> InsertPageModel(PageModel model, string storeId);
    Task<Page> UpdatePageModel(Page page, PageModel model, string storeId);
    Task<Page> CopyPageModel(string sourcePageId, string storeId);
    Task DeletePage(Page page);
}
