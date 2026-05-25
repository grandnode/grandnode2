namespace Grand.Web.Store.Models;

public class StoreWarehouseModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public int DisplayOrder { get; set; }
    public string StoreId { get; set; }
    public bool IsAssignedToCurrentStore { get; set; }
}
