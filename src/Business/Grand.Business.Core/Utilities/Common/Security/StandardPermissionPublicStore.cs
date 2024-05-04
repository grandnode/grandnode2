using Grand.Domain.Permissions;

namespace Grand.Business.Core.Utilities.Common.Security;

public static partial class StandardPermission
{
    public static readonly Permission DisplayPrices = new() {
        Name = "Display Prices",
        SystemName = PermissionSystemName.DisplayPrices,
        Area = "Public store",
        Category = CategoryPublicStore
    };

    public static readonly Permission EnableShoppingCart = new() {
        Name = "Enable shopping cart",
        SystemName = PermissionSystemName.EnableShoppingCart,
        Area = "Public store",
        Category = CategoryPublicStore
    };

    public static readonly Permission EnableWishlist = new() {
        Name = "Enable wishlist",
        SystemName = PermissionSystemName.EnableWishlist,
        Area = "Public store",
        Category = CategoryPublicStore
    };

    public static readonly Permission PublicStoreAllowNavigation = new() {
        Name = "Allow navigation",
        SystemName = PermissionSystemName.PublicStoreAllowNavigation,
        Area = "Public store",
        Category = CategoryPublicStore
    };

    public static readonly Permission AccessClosedStore = new() {
        Name = "Access a closed store",
        SystemName = PermissionSystemName.AccessClosedStore,
        Area = "Public store",
        Category = CategoryPublicStore
    };

    public static readonly Permission AllowUseApi = new() {
        Name = "Allow to use api for web",
        SystemName = PermissionSystemName.AllowUseApi,
        Area = "Public store",
        Category = CategoryPublicStore
    };

    private static string CategoryPublicStore => "PublicStore";
}