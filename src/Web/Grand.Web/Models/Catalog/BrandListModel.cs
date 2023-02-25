namespace Grand.Web.Models.Catalog;

public class BrandListModel
{
    public BrandListModel()
    {
        PagingModel = new BrandPagingModel();
        BrandsModel = new List<BrandModel>();
    }
    public BrandPagingModel PagingModel { get; set; }
    public IList<BrandModel> BrandsModel { get; set; }
}