using Grand.Domain.Permissions;

namespace Grand.Business.Core.Utilities.Common.Security;

public static partial class StandardPermission
{
    public static readonly Permission ManageOrders = new() {
        Name = "Manage Orders",
        SystemName = PermissionSystemName.Orders,
        Area = "Admin area",
        Category = "Orders",
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Edit, PermissionActionName.Payments,
            PermissionActionName.Cancel, PermissionActionName.Preview, PermissionActionName.Delete,
            PermissionActionName.Export
        }
    };

    public static readonly Permission ManageOrderTags = new() {
        Name = "Manage Order Tags",
        SystemName = PermissionSystemName.OrderTags,
        Area = "Admin area",
        Category = CategoryOrder,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Edit, PermissionActionName.Preview,
            PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageOrderStatus = new() {
        Name = "Manage Order status",
        SystemName = PermissionSystemName.OrderStatus,
        Area = "Admin area",
        Category = CategoryOrder
    };

    public static readonly Permission ManageShipments = new() {
        Name = "Manage Shipments",
        SystemName = PermissionSystemName.Shipments,
        Area = "Admin area",
        Category = CategoryOrder,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete, PermissionActionName.Export
        }
    };

    public static readonly Permission ManageGiftVouchers = new() {
        Name = "Manage Gift vouchers",
        SystemName = PermissionSystemName.GiftVouchers,
        Area = "Admin area",
        Category = CategoryOrder,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageMerchandiseReturns = new() {
        Name = "Manage Merchandise Returns",
        SystemName = PermissionSystemName.MerchandiseReturns,
        Area = "Admin area",
        Category = CategoryOrder,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManagePaymentTransactions = new() {
        Name = "Manage Payment Transaction",
        SystemName = PermissionSystemName.PaymentTransactions,
        Area = "Admin area",
        Category = CategoryOrder,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    public static readonly Permission ManageCurrentCarts = new() {
        Name = "Manage Current Carts",
        SystemName = PermissionSystemName.CurrentCarts,
        Area = "Admin area",
        Category = CategoryOrder
    };

    public static readonly Permission ManageCheckoutAttribute = new() {
        Name = "Manage Checkout Attributes",
        SystemName = PermissionSystemName.CheckoutAttributes,
        Area = "Admin area",
        Category = CategoryOrder,
        Actions = new List<string> {
            PermissionActionName.List, PermissionActionName.Create, PermissionActionName.Edit,
            PermissionActionName.Preview, PermissionActionName.Delete
        }
    };

    private static string CategoryOrder => "Orders";
}