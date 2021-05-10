using Grand.Domain.Permissions;
using System.Collections.Generic;

namespace Grand.Business.Common.Services.Security
{
    public static partial class StandardPermission
    {
        private static string CategoryContent => "Content";

        public static readonly Permission ManageNews = new Permission 
        { 
            Name = "Manage News", 
            SystemName = PermissionSystemName.News,
            Area = "Admin area",
            Category = CategoryContent, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete } 
        };
        public static readonly Permission ManageBlog = new Permission 
        { 
            Name = "Manage Blog", 
            SystemName = PermissionSystemName.Blog,
            Area = "Admin area",
            Category = CategoryContent, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete } 
        };
        public static readonly Permission ManageWidgets = new Permission 
        { 
            Name = "Manage Widgets", 
            SystemName = PermissionSystemName.Widgets,
            Area = "Admin area",
            Category = CategoryContent, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Edit } 
        };
        public static readonly Permission ManagePages = new Permission 
        { 
            Name = "Manage Pages", 
            SystemName = PermissionSystemName.Pages,
            Area = "Admin area",
            Category = CategoryContent, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete } 
        };
        public static readonly Permission ManageKnowledgebase = new Permission 
        { 
            Name = "Manage Knowledgebase", 
            SystemName = PermissionSystemName.Knowledgebase,
            Area = "Admin area",
            Category = CategoryContent, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete } 
        };
        public static readonly Permission ManageCourses = new Permission 
        { 
            Name = "Manage Courses", 
            SystemName = PermissionSystemName.Courses,
            Area = "Admin area",
            Category = CategoryContent, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete } 
        };
        public static readonly Permission ManageMessageTemplates = new Permission 
        { 
            Name = "Manage Message Templates", 
            SystemName = PermissionSystemName.MessageTemplates,
            Area = "Admin area",
            Category = CategoryContent, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete } 
        };

    }
}
