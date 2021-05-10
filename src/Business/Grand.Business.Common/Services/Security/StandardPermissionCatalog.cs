using Grand.Domain.Permissions;
using System.Collections.Generic;

namespace Grand.Business.Common.Services.Security
{
    public static partial class StandardPermission
    {
        private static string CategoryCatalog => "Catalog";

        public static readonly Permission ManageProducts = new Permission 
        { 
            Name = "Manage Products", 
            SystemName = PermissionSystemName.Products,
            Area = "Admin area",
            Category = CategoryCatalog, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete, PermissionActionName.Export, PermissionActionName.Import } 
        };
        public static readonly Permission ManageCategories = new Permission 
        { 
            Name = "Manage Categories", 
            SystemName = PermissionSystemName.Categories,
            Area = "Admin area",
            Category = CategoryCatalog, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete, PermissionActionName.Export, PermissionActionName.Import } 
        };
        public static readonly Permission ManageBrands = new Permission
        {
            Name = "Manage Brands",
            SystemName = PermissionSystemName.Brands,
            Area = "Admin area",
            Category = CategoryCatalog,
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete, PermissionActionName.Export, PermissionActionName.Import }
        };
        public static readonly Permission ManageCollections = new Permission 
        { 
            Name = "Manage Collections", 
            SystemName = PermissionSystemName.Collections,
            Area = "Admin area",
            Category = CategoryCatalog, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete, PermissionActionName.Export, PermissionActionName.Import } 
        };
        public static readonly Permission ManageProductReviews = new Permission 
        { 
            Name = "Manage Product Reviews", 
            SystemName = PermissionSystemName.ProductReviews,
            Area = "Admin area",
            Category = CategoryCatalog, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete } 
        };
        public static readonly Permission ManageProductTags = new Permission 
        { 
            Name = "Manage Product Tags", 
            SystemName = PermissionSystemName.ProductTags,
            Area = "Admin area",
            Category = CategoryCatalog, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete } 
        };
        public static readonly Permission ManageProductAttributes = new Permission 
        { 
            Name = "Manage Product Attributes", 
            SystemName = PermissionSystemName.ProductAttributes,
            Area = "Admin area",
            Category = CategoryCatalog, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete } 
        };
        public static readonly Permission ManageSpecificationAttributes = new Permission 
        { 
            Name = "Manage Specification Attributes", 
            SystemName = PermissionSystemName.SpecificationAttributes,
            Area = "Admin area",
            Category = CategoryCatalog, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete } 
        };
        public static readonly Permission ManageContactAttribute = new Permission 
        { 
            Name = "Manage Contact Attribute", 
            SystemName = PermissionSystemName.ContactAttributes,
            Area = "Admin area",
            Category = CategoryCatalog, 
            Actions = new List<string> { PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit, PermissionActionName.Preview, PermissionActionName.Delete } 
        };

    }
}
