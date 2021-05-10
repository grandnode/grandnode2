using Grand.Domain.Customers;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Customers
{
    public partial class CustomerActionConditionModel : BaseEntityModel
    {
        public CustomerActionConditionModel()
        {
            this.CustomerActionConditionType = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Admin.Customers.CustomerActionCondition.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerActionCondition.Fields.CustomerConditionTypeId")]
        public CustomerActionConditionTypeEnum CustomerActionConditionTypeId { get; set; }
        public IList<SelectListItem> CustomerActionConditionType { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerActionCondition.Fields.ConditionId")]
        public CustomerActionConditionEnum ConditionId { get; set; }

        public string CustomerActionId { get; set; }


        public partial class AddProductToConditionModel
        {
            public AddProductToConditionModel()
            {
                AvailableStores = new List<SelectListItem>();
                AvailableVendors = new List<SelectListItem>();
                AvailableProductTypes = new List<SelectListItem>();
            }

            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductName")]

            public string SearchProductName { get; set; }
            [UIHint("Category")]
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCategory")]
            public string SearchCategoryId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.Brand")]
            [UIHint("Brand")]
            public string SearchBrandId { get; set; }
            [UIHint("Collection")]
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCollection")]
            public string SearchCollectionId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchStore")]
            public string SearchStoreId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchVendor")]
            public string SearchVendorId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductType")]
            public int SearchProductTypeId { get; set; }

            public IList<SelectListItem> AvailableStores { get; set; }
            public IList<SelectListItem> AvailableVendors { get; set; }
            public IList<SelectListItem> AvailableProductTypes { get; set; }

            public string CustomerActionId { get; set; }
            public string CustomerActionConditionId { get; set; }

            public string[] SelectedProductIds { get; set; }
        }

        public partial class AddCategoryConditionModel
        {
            [GrandResourceDisplayName("Admin.Catalog.Categories.List.SearchCategoryName")]

            public string SearchCategoryName { get; set; }

            public string CustomerActionId { get; set; }
            public string CustomerActionConditionId { get; set; }

            public string[] SelectedCategoryIds { get; set; }
        }

        public partial class AddCollectionConditionModel
        {
            [GrandResourceDisplayName("Admin.Catalog.Collections.List.SearchCollectionName")]

            public string SearchCollectionName { get; set; }

            public string CustomerActionId { get; set; }
            public string CustomerActionConditionId { get; set; }

            public string[] SelectedCollectionIds { get; set; }
        }

        public partial class AddVendorConditionModel
        {
            public string CustomerActionId { get; set; }
            public string ConditionId { get; set; }

            public string VendorId { get; set; }
            public string Id { get; set; }
        }

        public partial class AddCustomerGroupConditionModel
        {
            public string CustomerActionId { get; set; }
            public string ConditionId { get; set; }

            public string CustomerGroupId { get; set; }
            public string Id { get; set; }
        }

        public partial class AddStoreConditionModel
        {
            public string CustomerActionId { get; set; }
            public string ConditionId { get; set; }

            public string StoreId { get; set; }
            public string Id { get; set; }
        }

        public partial class AddCustomerTagConditionModel
        {
            public string CustomerActionId { get; set; }
            public string ConditionId { get; set; }

            public string CustomerTagId { get; set; }
            public string Id { get; set; }
        }

        public partial class AddProductAttributeConditionModel
        {
            public string CustomerActionId { get; set; }
            public string ConditionId { get; set; }
            public string ProductAttributeId { get; set; }
            public string Name { get; set; }
            public string Id { get; set; }
        }
        public partial class AddUrlConditionModel
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string CustomerActionId { get; set; }
            public string ConditionId { get; set; }
        }

        public partial class AddCustomerRegisterConditionModel
        {
            public string CustomerActionId { get; set; }
            public string ConditionId { get; set; }
            public string CustomerRegisterName { get; set; }
            public string CustomerRegisterValue { get; set; }
            public string Id { get; set; }
        }

        public partial class AddCustomCustomerAttributeConditionModel
        {
            public string CustomerActionId { get; set; }
            public string ConditionId { get; set; }
            public string CustomerAttributeName { get; set; }
            public string CustomerAttributeValue { get; set; }
            public string Id { get; set; }
        }

        public partial class AddProductSpecificationConditionModel
        {
            public string CustomerActionId { get; set; }
            public string ConditionId { get; set; }
            public string SpecificationId { get; set; }
            public string SpecificationValueId { get; set; }
            public string Name { get; set; }
            public string Id { get; set; }
        }

    }
}