namespace Grand.Web.Models.Catalog;

public class VendorListModel
{
    public VendorPagingModel PagingModel { get; set; } = new();
    public IList<VendorModel> VendorsModel { get; set; } = new List<VendorModel>();
}