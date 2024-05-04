using Grand.Domain.Permissions;

namespace Grand.Business.Core.Utilities.Common.Security;

public static partial class StandardPermission
{
    public static readonly Permission ManageCountries = new() {
        Name = "Manage Countries",
        SystemName = PermissionSystemName.Countries,
        Area = "Admin area",
        Category = CategoryConfiguration,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete, PermissionActionName.Export,
            PermissionActionName.Import
        }
    };

    public static readonly Permission ManageLanguages = new() {
        Name = "Manage Languages",
        SystemName = PermissionSystemName.Languages,
        Area = "Admin area",
        Category = CategoryConfiguration,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete, PermissionActionName.Export,
            PermissionActionName.Import
        }
    };

    public static readonly Permission ManageSettings = new() {
        Name = "Manage Settings",
        SystemName = PermissionSystemName.Settings,
        Area = "Admin area",
        Category = CategoryConfiguration
    };

    public static readonly Permission ManagePaymentMethods = new() {
        Name = "Manage Payment Methods",
        SystemName = PermissionSystemName.PaymentMethods,
        Area = "Admin area",
        Category = CategoryConfiguration
    };

    public static readonly Permission ManageExternalAuthenticationMethods = new() {
        Name = "Manage External Authentication Methods",
        SystemName = PermissionSystemName.ExternalAuthenticationMethods,
        Area = "Admin area",
        Category = CategoryConfiguration
    };

    public static readonly Permission ManageTaxSettings = new() {
        Name = "Manage Tax Settings",
        SystemName = PermissionSystemName.TaxSettings,
        Area = "Admin area",
        Category = CategoryConfiguration
    };

    public static readonly Permission ManageShippingSettings = new() {
        Name = "Manage Shipping Settings",
        SystemName = PermissionSystemName.ShippingSettings,
        Category = CategoryConfiguration
    };

    public static readonly Permission ManageCurrencies = new() {
        Name = "Manage Currencies",
        SystemName = PermissionSystemName.Currencies,
        Area = "Admin area",
        Category = CategoryConfiguration,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageMeasures = new() {
        Name = "Manage Measures",
        SystemName = PermissionSystemName.Measures,
        Area = "Admin area",
        Category = CategoryConfiguration,
        Actions = new List<string> {
            PermissionActionName.Weights_List, PermissionActionName.Weights_Add, PermissionActionName.Weights_Edit,
            PermissionActionName.Weights_Delete,
            PermissionActionName.Units_List, PermissionActionName.Units_Add, PermissionActionName.Units_Edit,
            PermissionActionName.Units_Delete,
            PermissionActionName.Dimensions_List, PermissionActionName.Dimensions_Add,
            PermissionActionName.Dimensions_Edit, PermissionActionName.Dimensions_Delete
        }
    };

    public static readonly Permission ManageAcl = new() {
        Name = "Manage ACL",
        SystemName = PermissionSystemName.Acl,
        Area = "Admin area",
        Category = CategoryConfiguration
    };

    public static readonly Permission ManageEmailAccounts = new() {
        Name = "Manage Email Accounts",
        SystemName = PermissionSystemName.EmailAccounts,
        Area = "Admin area",
        Category = CategoryConfiguration,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Edit, PermissionActionName.Create,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageStores = new() {
        Name = "Manage Stores",
        SystemName = PermissionSystemName.Stores,
        Area = "Admin area",
        Category = CategoryConfiguration,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Edit, PermissionActionName.Create,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManagePlugins = new() {
        Name = "Manage Plugins",
        SystemName = PermissionSystemName.Plugins,
        Area = "Admin area",
        Category = CategoryConfiguration
    };

    private static string CategoryConfiguration => "Configuration";
}