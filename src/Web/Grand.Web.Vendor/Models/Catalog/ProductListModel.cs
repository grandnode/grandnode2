using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Vendor.Models.Catalog;

public class ProductListModel : BaseModel
{
    [GrandResourceDisplayName("Vendor.Catalog.Products.List.SearchProductName")]
    public string SearchProductName { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.List.SearchCategory")]
    [UIHint("Category")]
    public string SearchCategoryId { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.List.SearchIncludeSubCategories")]
    public bool SearchIncludeSubCategories { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.List.Brand")]
    [UIHint("Brand")]
    public string SearchBrandId { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.List.SearchCollection")]
    [UIHint("Collection")]
    public string SearchCollectionId { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.List.SearchWarehouse")]
    public string SearchWarehouseId { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.List.SearchProductType")]
    public int SearchProductTypeId { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.List.SearchPublished")]
    public int SearchPublishedId { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.List.GoDirectlyToSku")]

    public string GoDirectlyToSku { get; set; }

    public IList<SelectListItem> AvailableWarehouses { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableProductTypes { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailablePublishedOptions { get; set; } = new List<SelectListItem>();
}