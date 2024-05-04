using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Menu;

public class MenuModel : BaseEntityModel
{
    /// <summary>
    ///     Gets or sets the system name.
    /// </summary>
    [GrandResourceDisplayName("admin.configuration.menu.fields.systemname")]
    public string SystemName { get; set; }

    /// <summary>
    ///     Gets or sets the resource name.
    /// </summary>
    [GrandResourceDisplayName("admin.configuration.menu.fields.resourcename")]
    public string ResourceName { get; set; }

    /// <summary>
    ///     Gets or sets the name of the controller.
    /// </summary>
    [GrandResourceDisplayName("admin.configuration.menu.fields.controllername")]
    public string ControllerName { get; set; }

    /// <summary>
    ///     Gets or sets the name of the action.
    /// </summary>
    [GrandResourceDisplayName("admin.configuration.menu.fields.actionname")]
    public string ActionName { get; set; }

    /// <summary>
    ///     Gets or sets the URL.
    /// </summary>
    [GrandResourceDisplayName("admin.configuration.menu.fields.url")]
    public string Url { get; set; }

    /// <summary>
    ///     Gets or sets the icon class
    /// </summary>
    [GrandResourceDisplayName("admin.configuration.menu.fields.iconclass")]
    public string IconClass { get; set; }

    /// <summary>
    ///     Gets or sets the display order
    /// </summary>
    [GrandResourceDisplayName("admin.configuration.menu.fields.displayorder")]
    public int DisplayOrder { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to open url in new tab (window) or not
    /// </summary>
    [GrandResourceDisplayName("admin.configuration.menu.fields.openurlinnewtab")]
    public bool OpenUrlInNewTab { get; set; }

    /// <summary>
    ///     Gets or sets permissions
    /// </summary>
    public IList<string> PermissionNames { get; set; } = new List<string>();

    /// <summary>
    ///     Gets or sets all permissions
    /// </summary>
    [GrandResourceDisplayName("admin.configuration.menu.fields.allpermissions")]
    public bool AllPermissions { get; set; }
}