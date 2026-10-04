namespace Grand.Domain.Permissions;

public static partial class StandardPermission
{
    public static readonly Permission DisplayPrices = new() {
        Name = "Display Prices",
        SystemName = PermissionSystemName.DisplayPrices,
        Area = "Public store",
        Category = CategoryPublicStore
    };

    public static readonly Permission EnableShoppingCart = new() {
        Name = "Enable Shopping Cart",
        SystemName = PermissionSystemName.EnableShoppingCart,
        Area = "Public store",
        Category = CategoryPublicStore
    };

    public static readonly Permission EnableWishlist = new() {
        Name = "Enable Wishlist",
        SystemName = PermissionSystemName.EnableWishlist,
        Area = "Public store",
        Category = CategoryPublicStore
    };

    public static readonly Permission PublicStoreAllowNavigation = new() {
        Name = "Allow Navigation",
        SystemName = PermissionSystemName.PublicStoreAllowNavigation,
        Area = "Public store",
        Category = CategoryPublicStore
    };

    public static readonly Permission AccessClosedStore = new() {
        Name = "Access Closed Store",
        SystemName = PermissionSystemName.AccessClosedStore,
        Area = "Public store",
        Category = CategoryPublicStore
    };

    public static readonly Permission AllowUseApi = new() {
        Name = "Allow Web API Access",
        SystemName = PermissionSystemName.AllowUseApi,
        Area = "Public store",
        Category = CategoryPublicStore
    };

    private static string CategoryPublicStore => "PublicStore";
}