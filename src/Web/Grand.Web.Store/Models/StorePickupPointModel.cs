namespace Grand.Web.Store.Models;

public class StorePickupPointModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsAssignedToCurrentStore { get; set; }
    public bool CanManage { get; set; }
}
