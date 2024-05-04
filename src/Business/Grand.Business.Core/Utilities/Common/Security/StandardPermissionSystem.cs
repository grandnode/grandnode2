using Grand.Domain.Permissions;

namespace Grand.Business.Core.Utilities.Common.Security;

public static partial class StandardPermission
{
    public static readonly Permission ManageMessageQueue = new() {
        Name = "Manage Message Queue",
        SystemName = PermissionSystemName.MessageQueue,
        Area = "Admin area",
        Category = CategorySystem,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageMessageContactForm = new() {
        Name = "Manage Message Contact form",
        SystemName = PermissionSystemName.MessageContactForm,
        Area = "Admin area",
        Category = CategorySystem,
        Actions = new List<string>
            { PermissionActionName.List, PermissionActionName.Preview, PermissionActionName.Delete }
    };

    public static readonly Permission ManageMaintenance = new() {
        Name = "Manage Maintenance",
        SystemName = PermissionSystemName.Maintenance,
        Area = "Admin area",
        Category = CategorySystem
    };

    public static readonly Permission ManageSystem = new() {
        Name = "Manage System",
        SystemName = PermissionSystemName.System,
        Area = "Admin area",
        Category = CategorySystem
    };

    public static readonly Permission ManageFiles = new() {
        Name = "Manage Files",
        SystemName = PermissionSystemName.Files,
        Area = "Admin area",
        Category = CategorySystem
    };

    public static readonly Permission ManagePictures = new() {
        Name = "Manage Pictures",
        SystemName = PermissionSystemName.Pictures,
        Area = "Admin area",
        Category = CategorySystem
    };

    public static readonly Permission HtmlEditorManagePictures = new() {
        Name = "HTML Editor. Manage pictures",
        SystemName = PermissionSystemName.HtmlEditor,
        Area = "Admin area",
        Category = "Configuration"
    };

    public static readonly Permission ManageScheduleTasks = new() {
        Name = "Manage Schedule Tasks",
        SystemName = PermissionSystemName.ScheduleTasks,
        Area = "Admin area",
        Category = CategorySystem,
        Actions = new List<string>
            { PermissionActionName.List, PermissionActionName.Edit, PermissionActionName.Preview }
    };

    private static string CategorySystem => "System";
}