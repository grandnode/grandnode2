using Grand.Domain.Permissions;

namespace Grand.Business.Core.Utilities.Common.Security;

public static partial class StandardPermission
{
    public static readonly Permission ManageAffiliates = new() {
        Name = "Manage Affiliates",
        SystemName = PermissionSystemName.Affiliates,
        Area = "Admin area",
        Category = CategoryMarketing,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManagePushEvents = new() {
        Name = "Manage Push Events",
        SystemName = PermissionSystemName.PushNotifications,
        Area = "Admin area",
        Category = CategoryMarketing,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageCampaigns = new() {
        Name = "Manage Campaigns",
        SystemName = PermissionSystemName.Campaigns,
        Area = "Admin area",
        Category = CategoryMarketing,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete, PermissionActionName.Export
        }
    };

    public static readonly Permission ManageDiscounts = new() {
        Name = "Manage Discounts",
        SystemName = PermissionSystemName.Discounts,
        Area = "Admin area",
        Category = CategoryMarketing,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Preview,
            PermissionActionName.Edit, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageNewsletterSubscribers = new() {
        Name = "Manage Newsletter Subscribers",
        SystemName = PermissionSystemName.NewsletterSubscribers,
        Area = "Admin area",
        Category = CategoryMarketing,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Edit, PermissionActionName.Export,
            PermissionActionName.Import, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageNewsletterCategories = new() {
        Name = "Manage Newsletter Categories",
        SystemName = PermissionSystemName.NewsletterCategories,
        Area = "Admin area",
        Category = CategoryMarketing,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Preview,
            PermissionActionName.Edit, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageDocuments = new() {
        Name = "Manage Documents",
        SystemName = PermissionSystemName.Documents,
        Area = "Admin area",
        Category = CategoryMarketing,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    private static string CategoryMarketing => "Marketing";
}