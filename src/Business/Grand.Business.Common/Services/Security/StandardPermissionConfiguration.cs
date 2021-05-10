using Grand.Domain.Permissions;
using System.Collections.Generic;

namespace Grand.Business.Common.Services.Security
{
    public static partial class StandardPermission
    {
        private static string CategoryConfiguration => "Configuration";

        public static readonly Permission ManageCountries = new Permission
        {
            Name = "Manage Countries",
            SystemName = PermissionSystemName.Countries,
            Area = "Admin area",
            Category = CategoryConfiguration,
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete, PermissionActionName.Export, PermissionActionName.Import }
        };

        public static readonly Permission ManageLanguages = new Permission
        {
            Name = "Manage Languages",
            SystemName = PermissionSystemName.Languages,
            Area = "Admin area",
            Category = CategoryConfiguration,
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete, PermissionActionName.Export, PermissionActionName.Import }
        };
        public static readonly Permission ManageSettings = new Permission 
        { 
            Name = "Manage Settings", 
            SystemName = PermissionSystemName.Settings,
            Area = "Admin area",
            Category = CategoryConfiguration
        };
        public static readonly Permission ManagePaymentMethods = new Permission 
        { 
            Name = "Manage Payment Methods", 
            SystemName = PermissionSystemName.PaymentMethods,
            Area = "Admin area",
            Category = CategoryConfiguration
        };
        public static readonly Permission ManageExternalAuthenticationMethods = new Permission 
        { 
            Name = "Manage External Authentication Methods", 
            SystemName = PermissionSystemName.ExternalAuthenticationMethods,
            Area = "Admin area",
            Category = CategoryConfiguration
        };
        public static readonly Permission ManageTaxSettings = new Permission 
        { 
            Name = "Manage Tax Settings", 
            SystemName = PermissionSystemName.TaxSettings,
            Area = "Admin area",
            Category = CategoryConfiguration
        };
        public static readonly Permission ManageShippingSettings = new Permission 
        { 
            Name = "Manage Shipping Settings", 
            SystemName = PermissionSystemName.ShippingSettings, 
            Category = CategoryConfiguration
        };
        public static readonly Permission ManageCurrencies = new Permission 
        { 
            Name = "Manage Currencies", 
            SystemName = PermissionSystemName.Currencies,
            Area = "Admin area",
            Category = CategoryConfiguration, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete } 
        };
        public static readonly Permission ManageMeasures = new Permission
        {
            Name = "Manage Measures",
            SystemName = PermissionSystemName.Measures,
            Area = "Admin area",
            Category = CategoryConfiguration,
            Actions = new List<string> {
                PermissionActionName.Weights_List, PermissionActionName.Weights_Add, PermissionActionName.Weights_Edit, PermissionActionName.Weights_Delete,
                PermissionActionName.Units_List, PermissionActionName.Units_Add, PermissionActionName.Units_Edit, PermissionActionName.Units_Delete,
                PermissionActionName.Dimensions_List, PermissionActionName.Dimensions_Add, PermissionActionName.Dimensions_Edit, PermissionActionName.Dimensions_Delete,
            }
        };
        public static readonly Permission ManageActivityLog = new Permission 
        { 
            Name = "Manage Activity Log", 
            SystemName = PermissionSystemName.ActivityLog,
            Area = "Admin area",
            Category = CategoryConfiguration, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Edit, PermissionActionName.Delete } 
        };
        public static readonly Permission ManageAcl = new Permission 
        { 
            Name = "Manage ACL", 
            SystemName = PermissionSystemName.Acl,
            Area = "Admin area",
            Category = CategoryConfiguration
        };
        public static readonly Permission ManageEmailAccounts = new Permission 
        { 
            Name = "Manage Email Accounts", 
            SystemName = PermissionSystemName.EmailAccounts,
            Area = "Admin area",
            Category = CategoryConfiguration, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Edit, PermissionActionName.Create, PermissionActionName.Preview, PermissionActionName.Delete } 
        };
        public static readonly Permission ManageStores = new Permission 
        { 
            Name = "Manage Stores", 
            SystemName = PermissionSystemName.Stores,
            Area = "Admin area",
            Category = CategoryConfiguration, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Edit, PermissionActionName.Create, PermissionActionName.Preview, PermissionActionName.Delete } 
        };
        public static readonly Permission ManagePlugins = new Permission 
        { 
            Name = "Manage Plugins", 
            SystemName = PermissionSystemName.Plugins,
            Area = "Admin area",
            Category = CategoryConfiguration
        };
        
    }
}
