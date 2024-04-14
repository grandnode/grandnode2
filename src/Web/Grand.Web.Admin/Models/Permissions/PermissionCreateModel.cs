using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Permissions;

public class PermissionCreateModel : BaseModel
{
    /// <summary>
    ///     Gets or sets the permission name
    /// </summary>
    [GrandResourceDisplayName("Admin.Configuration.Permissions.Fields.Name")]
    public string Name { get; set; }

    /// <summary>
    ///     Gets or sets the permission system name
    /// </summary>
    [GrandResourceDisplayName("Admin.Configuration.Permissions.Fields.SystemName")]
    public string SystemName { get; set; }

    /// <summary>
    ///     Gets or sets the area name
    /// </summary>
    [GrandResourceDisplayName("Admin.Configuration.Permissions.Fields.Area")]
    public string Area { get; set; }

    /// <summary>
    ///     Gets or sets the permission category
    /// </summary>
    [GrandResourceDisplayName("Admin.Configuration.Permissions.Fields.Category")]
    public string Category { get; set; }
}