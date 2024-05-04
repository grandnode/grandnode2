using Grand.Domain.Permissions;

namespace Grand.Business.Core.Utilities.Common.Security;

public static partial class StandardPermission
{
    public static readonly Permission AllowCustomerImpersonation = new() {
        Name = "Allow Customer Impersonation",
        SystemName = PermissionSystemName.AllowCustomerImpersonation,
        Area = "Admin area",
        Category = CategoryCustomer
    };

    public static readonly Permission ManageCustomers = new() {
        Name = "Manage Customers",
        SystemName = PermissionSystemName.Customers,
        Area = "Admin area",
        Category = CategoryCustomer,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete, PermissionActionName.Export
        }
    };

    public static readonly Permission ManageAddressAttribute = new() {
        Name = "Manage Address Attributes",
        SystemName = PermissionSystemName.AddressAttributes,
        Area = "Admin area",
        Category = CategoryCustomer,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageCustomerAttribute = new() {
        Name = "Manage Customer Attributes",
        SystemName = PermissionSystemName.CustomerAttributes,
        Area = "Admin area",
        Category = CategoryCustomer,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageCustomerGroups = new() {
        Name = "Manage Customer Groups",
        SystemName = PermissionSystemName.CustomerGroups,
        Area = "Admin area",
        Category = CategoryCustomer,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageCustomerTags = new() {
        Name = "Manage Customer Tags",
        SystemName = PermissionSystemName.CustomerTags,
        Area = "Admin area",
        Category = CategoryCustomer,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageSalesEmployees = new() {
        Name = "Manage Sales Employees",
        SystemName = PermissionSystemName.SalesEmployees,
        Area = "Admin area",
        Category = CategoryCustomer,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageVendors = new() {
        Name = "Manage Vendors",
        SystemName = PermissionSystemName.Vendors,
        Area = "Admin area",
        Category = CategoryCustomer,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageVendorReviews = new() {
        Name = "Manage Vendor Reviews",
        SystemName = PermissionSystemName.VendorReviews,
        Area = "Admin area",
        Category = CategoryCustomer,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    private static string CategoryCustomer => "Customers";
}