using Grand.Business.Core.Interfaces.System.Admin;
using Grand.Domain.Admin;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Menu;

namespace Grand.Web.Admin.Services;

public class MenuViewModelService : IMenuViewModelService
{
    private readonly IAdminSiteMapService _adminSiteMapService;

    public MenuViewModelService(IAdminSiteMapService adminSiteMapService)
    {
        _adminSiteMapService = adminSiteMapService;
    }

    public virtual async Task<IList<MenuListModel>> MenuItems()
    {
        var menuItems = await _adminSiteMapService.GetSiteMap();
        return menuItems.Select(x => new MenuListModel {
            Id = x.Id,
            SystemName = x.SystemName,
            DisplayOrder = x.DisplayOrder
        }).ToList();
    }

    public virtual async Task<AdminSiteMap> GetMenuById(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentNullException(nameof(id));

        var sitemap = await _adminSiteMapService.GetSiteMap();
        return sitemap.Select(rootNode => FindAdminSiteMapById(rootNode, id)).FirstOrDefault(result => result != null);
    }

    public virtual async Task<AdminSiteMap> FindParentNodeById(string id)
    {
        var node = await GetMenuById(id);
        if (node == null) return null;
        var sitemap = await _adminSiteMapService.GetSiteMap();
        return sitemap.Select(rootNode => FindParentNodeById(rootNode, id)).FirstOrDefault(result => result != null);
    }

    public virtual async Task<AdminSiteMap> InsertMenuModel(MenuModel model, string parentId)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (!string.IsNullOrEmpty(parentId)) return await AddChildNodeSiteMap();

        var entity = model.ToEntity();
        await _adminSiteMapService.InsertSiteMap(entity);
        return entity;

        async Task<AdminSiteMap> AddChildNodeSiteMap()
        {
            var sitemap = await _adminSiteMapService.GetSiteMap();
            var parentEntity = FindTopLevelNodeById(sitemap, parentId);
            var adminSiteMap = FindAdminSiteMapById(parentEntity, parentId);
            var siteMap = model.ToEntity();
            adminSiteMap.ChildNodes.Add(siteMap);
            await _adminSiteMapService.UpdateSiteMap(parentEntity);
            return siteMap;
        }
    }

    public virtual async Task<AdminSiteMap> UpdateMenuModel(MenuModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var sitemap = await _adminSiteMapService.GetSiteMap();
        var parentEntity = FindTopLevelNodeById(sitemap, model.Id);
        var adminSiteMap = FindAdminSiteMapById(parentEntity, model.Id);

        adminSiteMap = model.ToEntity(adminSiteMap);
        await _adminSiteMapService.UpdateSiteMap(parentEntity);

        return adminSiteMap;
    }

    public virtual async Task DeleteMenu(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentNullException(nameof(id));

        var sitemap = await _adminSiteMapService.GetSiteMap();
        var parentEntity = FindTopLevelNodeById(sitemap, id);
        if (parentEntity?.Id == id)
        {
            await _adminSiteMapService.DeleteSiteMap(parentEntity);
        }
        else
        {
            var adminSiteMap = FindParentNodeById(parentEntity, id);
            if (adminSiteMap != null)
            {
                adminSiteMap.ChildNodes.Remove(adminSiteMap.ChildNodes.FirstOrDefault(y => y.Id == id));
                await _adminSiteMapService.UpdateSiteMap(parentEntity);
            }
        }
    }

    private static AdminSiteMap FindTopLevelNodeById(IList<AdminSiteMap> adminSiteMaps, string targetId)
    {
        return adminSiteMaps.FirstOrDefault(adminSiteMap => GetAllIdsFromChildren(adminSiteMap).Contains(targetId));
    }

    private static List<string> GetAllIdsFromChildren(AdminSiteMap rootNode)
    {
        var allIds = new List<string>();
        GetAllIdsFromChildrenRecursive(rootNode, allIds);
        return allIds;
    }

    private static void GetAllIdsFromChildrenRecursive(AdminSiteMap currentNode, List<string> allIds)
    {
        if (currentNode == null) return;

        allIds.Add(currentNode.Id);

        if (currentNode.ChildNodes != null)
            foreach (var childNode in currentNode.ChildNodes)
                GetAllIdsFromChildrenRecursive(childNode, allIds);
    }

    private static AdminSiteMap FindAdminSiteMapById(AdminSiteMap rootNode, string id)
    {
        if (rootNode == null) return null;
        return rootNode.Id == id
            ? rootNode
            : rootNode.ChildNodes?.Select(childNode => FindAdminSiteMapById(childNode, id))
                .FirstOrDefault(result => result != null);
    }

    private static AdminSiteMap FindParentNodeById(AdminSiteMap rootNode, string childId)
    {
        if (rootNode == null) return null;
        if (rootNode.Id == childId) return rootNode;

        foreach (var childNode in rootNode.ChildNodes)
        {
            if (childNode.Id == childId) return rootNode;

            var foundNode = FindParentNodeById(childNode, childId);
            if (foundNode != null) return foundNode;
        }

        return null;
    }
}