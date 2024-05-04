using Grand.Domain.Permissions;

namespace Grand.Business.Core.Utilities.Common.Security;

public static partial class StandardPermission
{
    public static readonly Permission ManageNews = new() {
        Name = "Manage News",
        SystemName = PermissionSystemName.News,
        Area = "Admin area",
        Category = CategoryContent,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageBlog = new() {
        Name = "Manage Blog",
        SystemName = PermissionSystemName.Blog,
        Area = "Admin area",
        Category = CategoryContent,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageWidgets = new() {
        Name = "Manage Widgets",
        SystemName = PermissionSystemName.Widgets,
        Area = "Admin area",
        Category = CategoryContent,
        Actions = new List<string> { PermissionActionName.List, PermissionActionName.Edit }
    };

    public static readonly Permission ManagePages = new() {
        Name = "Manage Pages",
        SystemName = PermissionSystemName.Pages,
        Area = "Admin area",
        Category = CategoryContent,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageKnowledgebase = new() {
        Name = "Manage Knowledgebase",
        SystemName = PermissionSystemName.Knowledgebase,
        Area = "Admin area",
        Category = CategoryContent,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageCourses = new() {
        Name = "Manage Courses",
        SystemName = PermissionSystemName.Courses,
        Area = "Admin area",
        Category = CategoryContent,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageMessageTemplates = new() {
        Name = "Manage Message Templates",
        SystemName = PermissionSystemName.MessageTemplates,
        Area = "Admin area",
        Category = CategoryContent,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    private static string CategoryContent => "Content";
}