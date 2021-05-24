using Grand.Domain.Permissions;
using System.Collections.Generic;

namespace Grand.Business.Common.Services.Security
{
    public static partial class StandardPermission
    {
        private static string CategorySystem => "System";

        public static readonly Permission ManageSystemLog = new Permission
        {
            Name = "Manage System Log",
            SystemName = PermissionSystemName.SystemLog,
            Area = "Admin area",
            Category = CategorySystem
        };
        public static readonly Permission ManageMessageQueue = new Permission
        {
            Name = "Manage Message Queue",
            SystemName = PermissionSystemName.MessageQueue,
            Area = "Admin area",
            Category = CategorySystem,
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete }
        };
        public static readonly Permission ManageMessageContactForm = new Permission
        {
            Name = "Manage Message Contact form",
            SystemName = PermissionSystemName.MessageContactForm,
            Area = "Admin area",
            Category = CategorySystem,
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Preview, PermissionActionName.Delete }
        };
        public static readonly Permission ManageMaintenance = new Permission
        {
            Name = "Manage Maintenance",
            SystemName = PermissionSystemName.Maintenance,
            Area = "Admin area",
            Category = CategorySystem
        };
        public static readonly Permission ManageFiles = new Permission
        {
            Name = "Manage Files",
            SystemName = PermissionSystemName.Files,
            Area = "Admin area",
            Category = CategorySystem
        };
        public static readonly Permission ManagePictures = new Permission
        {
            Name = "Manage Pictures",
            SystemName = PermissionSystemName.Pictures,
            Area = "Admin area",
            Category = CategorySystem
        };
        public static readonly Permission ManageUserFields = new Permission
        {
            Name = "Manage user Fields",
            SystemName = PermissionSystemName.UserFields,
            Area = "Admin area",
            Category = CategorySystem
        };
        public static readonly Permission HtmlEditorManagePictures = new Permission
        {
            Name = "HTML Editor. Manage pictures",
            SystemName = PermissionSystemName.HtmlEditor,
            Area = "Admin area",
            Category = "Configuration"
        };
        public static readonly Permission ManageScheduleTasks = new Permission
        {
            Name = "Manage Schedule Tasks",
            SystemName = PermissionSystemName.ScheduleTasks,
            Area = "Admin area",
            Category = CategorySystem,
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Edit, PermissionActionName.Preview }
        };
        
    }
}
