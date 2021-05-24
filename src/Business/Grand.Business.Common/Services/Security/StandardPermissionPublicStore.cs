using Grand.Domain.Permissions;

namespace Grand.Business.Common.Services.Security
{
    public static partial class StandardPermission
    {
        private static string CategoryPublicStore => "PublicStore";

        public static readonly Permission DisplayPrices = new Permission
        {
            Name = "Display Prices",
            SystemName = PermissionSystemName.DisplayPrices,
            Area = "Public store",
            Category = CategoryPublicStore
        };
        public static readonly Permission EnableShoppingCart = new Permission
        {
            Name = "Enable shopping cart",
            SystemName = PermissionSystemName.EnableShoppingCart,
            Area = "Public store",
            Category = CategoryPublicStore
        };
        public static readonly Permission EnableWishlist = new Permission
        {
            Name = "Enable wishlist",
            SystemName = PermissionSystemName.EnableWishlist,
            Area = "Public store",
            Category = CategoryPublicStore
        };
        public static readonly Permission PublicStoreAllowNavigation = new Permission
        {
            Name = "Allow navigation",
            SystemName = PermissionSystemName.PublicStoreAllowNavigation,
            Area = "Public store",
            Category = CategoryPublicStore
        };
        public static readonly Permission AccessClosedStore = new Permission
        {
            Name = "Access a closed store",
            SystemName = PermissionSystemName.AccessClosedStore,
            Area = "Public store",
            Category = CategoryPublicStore
        };

        public static readonly Permission AllowUseApi = new Permission {
            Name = "Allow to use api for web",
            SystemName = PermissionSystemName.AllowUseApi,
            Area = "Public store",
            Category = CategoryPublicStore
        };
    }
}
