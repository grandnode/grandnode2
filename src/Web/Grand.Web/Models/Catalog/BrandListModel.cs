namespace Grand.Web.Models.Catalog;

public class BrandListModel
{
    public BrandPagingModel PagingModel { get; set; } = new();
    public IList<BrandModel> BrandsModel { get; set; } = new List<BrandModel>();
}