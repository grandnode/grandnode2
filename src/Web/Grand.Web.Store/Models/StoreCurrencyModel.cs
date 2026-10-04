namespace Grand.Web.Store.Models;

public class StoreCurrencyModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string CurrencyCode { get; set; }
    public bool Published { get; set; }
    public int DisplayOrder { get; set; }
    public bool LimitedToStores { get; set; }
    public bool IsAssignedToCurrentStore { get; set; }
    public bool IsPrimaryStoreCurrency { get; set; }
    public bool IsDefaultStoreCurrency { get; set; }
    public bool CanManage { get; set; }
}
