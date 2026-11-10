using Grand.Data;
using Grand.Domain.Admin;
using Grand.Domain.Permissions;

namespace Grand.Module.Migration.Migrations._2._5;

/// <summary>
///     The Inventory section of the admin menu the 2.5 migrations put their entries in; whichever of them runs first
///     creates it
/// </summary>
internal static class InventoryMenu
{
    public static async Task Add(IRepository<AdminSiteMap> repository,
        params (string SystemName, string ResourceName, string ControllerName, string ActionName, int DisplayOrder)[]
            nodes)
    {
        var inventory = await repository.FirstOrDefaultAsync(repository.Table.Where(x => x.SystemName == "Inventory"));
        var created = inventory == null;
        //right after Catalog, which shares its display order
        inventory ??= new AdminSiteMap {
            SystemName = "Inventory",
            ResourceName = "Admin.Inventory",
            PermissionNames = new List<string> { PermissionSystemName.InventoryDocuments },
            IconClass = "bi bi-box-seam",
            DisplayOrder = 1,
            ChildNodes = new List<AdminSiteMap>()
        };

        var changed = false;
        foreach (var node in nodes)
        {
            if (inventory.ChildNodes.Any(x => x.SystemName == node.SystemName))
                continue;

            inventory.ChildNodes.Add(new AdminSiteMap {
                SystemName = node.SystemName,
                ResourceName = node.ResourceName,
                PermissionNames = new List<string> { PermissionSystemName.InventoryDocuments },
                ControllerName = node.ControllerName,
                ActionName = node.ActionName,
                DisplayOrder = node.DisplayOrder,
                IconClass = "bi bi-record-circle"
            });
            changed = true;
        }

        if (created)
            await repository.InsertAsync(inventory);
        else if (changed)
            await repository.UpdateAsync(inventory);
    }
}
