using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Vendor.Models.Catalog;

public class BulkEditListModel : BaseModel
{
    [GrandResourceDisplayName("Vendor.Catalog.BulkEdit.List.SearchProductName")]
    public string SearchProductName { get; set; }

    [UIHint("Category")]
    [GrandResourceDisplayName("Vendor.Catalog.BulkEdit.List.SearchCategory")]
    public string SearchCategoryId { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.List.Brand")]
    [UIHint("Brand")]
    public string SearchBrandId { get; set; }

    [UIHint("Collection")]
    [GrandResourceDisplayName("Vendor.Catalog.BulkEdit.List.SearchCollection")]
    public string SearchCollectionId { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.List.SearchProductType")]
    public int SearchProductTypeId { get; set; }

    public IList<SelectListItem> AvailableProductTypes { get; set; } = new List<SelectListItem>();
}