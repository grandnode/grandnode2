using Grand.Domain.Permissions;
using System.Collections.Generic;

namespace Grand.Business.Common.Services.Security
{
    public static partial class StandardPermission
    {
        private static string CategoryMarketing => "Marketing";

        public static readonly Permission ManageAffiliates = new Permission 
        { 
            Name = "Manage Affiliates", 
            SystemName = PermissionSystemName.Affiliates,
            Area = "Admin area",
            Category = CategoryMarketing, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete } 
        };
        public static readonly Permission ManagePushEvents = new Permission 
        { 
            Name = "Manage Push Events", 
            SystemName = PermissionSystemName.PushNotifications,
            Area = "Admin area",
            Category = CategoryMarketing, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Delete } 
        };
        public static readonly Permission ManageCampaigns = new Permission 
        { 
            Name = "Manage Campaigns", 
            SystemName = PermissionSystemName.Campaigns,
            Area = "Admin area",
            Category = CategoryMarketing, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete, PermissionActionName.Export } 
        };
        public static readonly Permission ManageBanners = new Permission 
        { 
            Name = "Manage Banners", 
            SystemName = PermissionSystemName.Banners, 
            Category = CategoryMarketing, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete } 
        };
        public static readonly Permission ManageInteractiveForm = new Permission 
        { 
            Name = "Manage Interactive Forms", 
            SystemName = PermissionSystemName.InteractiveForms,
            Area = "Admin area",
            Category = CategoryMarketing, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete } 
        };
        public static readonly Permission ManageDiscounts = new Permission 
        { 
            Name = "Manage Discounts", 
            SystemName = PermissionSystemName.Discounts,
            Area = "Admin area",
            Category = CategoryMarketing, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Preview, PermissionActionName.Edit, PermissionActionName.Delete } 
        };
        public static readonly Permission ManageNewsletterSubscribers = new Permission 
        { 
            Name = "Manage Newsletter Subscribers", 
            SystemName = PermissionSystemName.NewsletterSubscribers,
            Area = "Admin area",
            Category = CategoryMarketing, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Edit, PermissionActionName.Export, PermissionActionName.Import, PermissionActionName.Delete } 
        };
        public static readonly Permission ManageNewsletterCategories = new Permission 
        { 
            Name = "Manage Newsletter Categories", 
            SystemName = PermissionSystemName.NewsletterCategories,
            Area = "Admin area",
            Category = CategoryMarketing, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Preview, PermissionActionName.Edit, PermissionActionName.Delete } 
        };
        public static readonly Permission ManageDocuments = new Permission
        {
            Name = "Manage Documents",
            SystemName = PermissionSystemName.Documents,
            Area = "Admin area",
            Category = CategoryMarketing,
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete }
        };

    }
}
