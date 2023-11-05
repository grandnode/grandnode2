namespace Grand.Web.Vendor.Models.Catalog;

public interface IProductValidVendor
{
    public string ProductId { get; set; }
}

public interface IProductRelatedValidVendor
{
    public string ProductId1 { get; set; }
    public string ProductId2 { get; set; }
}