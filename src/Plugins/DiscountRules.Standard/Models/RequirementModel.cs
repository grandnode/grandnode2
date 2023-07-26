using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace DiscountRules.Standard.Models;

public class RequirementCustomerGroupsModel
{
    public RequirementCustomerGroupsModel()
    {
        AvailableCustomerGroups = new List<SelectListItem>();
    }

    [GrandResourceDisplayName("Plugins.DiscountRules.CustomerGroups.Fields.CustomerGroup")]
    public string CustomerGroupId { get; set; }

    public string DiscountId { get; set; }

    public string RequirementId { get; set; }

    public IList<SelectListItem> AvailableCustomerGroups { get; set; }
}

public class RequirementSpentAmountModel
{
    [GrandResourceDisplayName("Plugins.DiscountRules.Standard.HadSpentAmount.Fields.Amount")]
    public double SpentAmount { get; set; }

    public string DiscountId { get; set; }

    public string RequirementId { get; set; }
}

public class RequirementAllProductsModel
{
    [GrandResourceDisplayName("Plugins.DiscountRules.HasAllProducts.Fields.Products")]
    public string Products { get; set; }

    public string DiscountId { get; set; }

    public string RequirementId { get; set; }

    #region Nested classes

    public class AddProductModel : BaseModel
    {
        public AddProductModel()
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
        [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCollection")]
        [UIHint("Collection")]
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

        //vendor
        public bool IsLoggedInAsVendor { get; set; }
    }

    public class ProductModel : BaseEntityModel
    {
        public string Name { get; set; }

        public bool Published { get; set; }
    }
    #endregion
}

public class RequirementOneProductModel
{
    [GrandResourceDisplayName("Plugins.DiscountRules.HasOneProduct.Fields.Products")]
    public string Products { get; set; }

    public string DiscountId { get; set; }

    public string RequirementId { get; set; }

    #region Nested classes

    public class AddProductModel : BaseModel
    {
        public AddProductModel()
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

        //vendor
        public bool IsLoggedInAsVendor { get; set; }
    }

    public class ProductModel : BaseEntityModel
    {
        public string Name { get; set; }

        public bool Published { get; set; }
    }

    #endregion
}

public class RequirementShoppingCartModel
{
    [GrandResourceDisplayName("Plugins.DiscountRules.ShoppingCart.Fields.Amount")]
    public double SpentAmount { get; set; }

    public string DiscountId { get; set; }

    public string RequirementId { get; set; }
}