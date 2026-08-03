namespace Grand.Web.Store.Models;

public class StoreLanguageModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string LanguageCulture { get; set; }
    public string FlagImageFileName { get; set; }
    public bool Published { get; set; }
    public int DisplayOrder { get; set; }
    public bool LimitedToStores { get; set; }
    public bool IsAssignedToCurrentStore { get; set; }
    public bool IsPrimaryStoreLanguage { get; set; }
    public bool IsDefaultStoreLanguage { get; set; }
    public bool CanManage { get; set; }
}
