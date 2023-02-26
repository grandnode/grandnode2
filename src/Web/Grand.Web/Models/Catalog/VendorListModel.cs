namespace Grand.Web.Models.Catalog;

public class VendorListModel
{
    public VendorListModel()
    {
        PagingModel = new VendorPagingModel();
        VendorsModel = new List<VendorModel>();
    }
    public VendorPagingModel PagingModel { get; set; }
    public IList<VendorModel> VendorsModel { get; set; }
}