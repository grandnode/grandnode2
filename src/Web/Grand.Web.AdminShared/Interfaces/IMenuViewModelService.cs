using Grand.Domain.Admin;
using Grand.Web.AdminShared.Models.Menu;

namespace Grand.Web.AdminShared.Interfaces;

public interface IMenuViewModelService
{
    Task<IList<MenuListModel>> MenuItems();
    Task<AdminSiteMap> GetMenuById(string id);
    Task<AdminSiteMap> FindParentNodeById(string id);
    Task<AdminSiteMap> InsertMenuModel(MenuModel model, string parentId);
    Task<AdminSiteMap> UpdateMenuModel(MenuModel model);
    Task DeleteMenu(string id);
}