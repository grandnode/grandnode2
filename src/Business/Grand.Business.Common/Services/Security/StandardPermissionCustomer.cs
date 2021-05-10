using Grand.Domain.Permissions;
using System.Collections.Generic;

namespace Grand.Business.Common.Services.Security
{
    public static partial class StandardPermission
    {
        private static string CategoryCustomer => "Customers";

        public static readonly Permission AllowCustomerImpersonation = new Permission
        {
            Name = "Allow Customer Impersonation",
            SystemName = PermissionSystemName.AllowCustomerImpersonation,
            Area = "Admin area",
            Category = CategoryCustomer
        };
        public static readonly Permission ManageCustomers = new Permission
        {
            Name = "Manage Customers",
            SystemName = PermissionSystemName.Customers,
            Area = "Admin area",
            Category = CategoryCustomer,
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete, PermissionActionName.Export }
        };
        public static readonly Permission ManageAddressAttribute = new Permission
        {
            Name = "Manage Address Attributes",
            SystemName = PermissionSystemName.AddressAttributes,
            Area = "Admin area",
            Category = CategoryCustomer,
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete }
        };
        public static readonly Permission ManageCustomerAttribute = new Permission
        {
            Name = "Manage Customer Attributes",
            SystemName = PermissionSystemName.CustomerAttributes,
            Area = "Admin area",
            Category = CategoryCustomer,
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete }
        };
        public static readonly Permission ManageCustomerGroups = new Permission
        {
            Name = "Manage Customer Groups",
            SystemName = PermissionSystemName.CustomerGroups,
            Area = "Admin area",
            Category = CategoryCustomer,
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete }
        };
        public static readonly Permission ManageCustomerTags = new Permission
        {
            Name = "Manage Customer Tags",
            SystemName = PermissionSystemName.CustomerTags,
            Area = "Admin area",
            Category = CategoryCustomer,
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete }
        };
        public static readonly Permission ManageSalesEmployees = new Permission
        {
            Name = "Manage Sales Employees",
            SystemName = PermissionSystemName.SalesEmployees,
            Area = "Admin area",
            Category = CategoryCustomer,
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Delete }
        };
        public static readonly Permission ManageActions = new Permission
        {
            Name = "Manage Customers Actions",
            SystemName = PermissionSystemName.Actions,
            Area = "Admin area",
            Category = CategoryCustomer
        };
        public static readonly Permission ManageReminders = new Permission
        {
            Name = "Manage Customers Reminders",
            SystemName = PermissionSystemName.Reminders,
            Area = "Admin area",
            Category = CategoryCustomer
        };
        public static readonly Permission ManageVendors = new Permission
        {
            Name = "Manage Vendors",
            SystemName = PermissionSystemName.Vendors,
            Area = "Admin area",
            Category = CategoryCustomer,
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete }
        };
        public static readonly Permission ManageVendorReviews = new Permission
        {
            Name = "Manage Vendor Reviews",
            SystemName = PermissionSystemName.VendorReviews,
            Area = "Admin area",
            Category = CategoryCustomer,
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete }
        };
        
    }
}
