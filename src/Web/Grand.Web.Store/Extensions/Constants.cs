namespace Grand.Web.Store.Extensions;

public static class Constants
{
    public const string AreaStore = "Store";
    public static string LayoutStore => $"~/Areas/{AreaStore}/Views/Shared/_StoreLayout.cshtml";
    public static string LayoutStoreLogin => $"~/Areas/{AreaStore}/Views/Shared/_StoreLoginLayout.cshtml";
}