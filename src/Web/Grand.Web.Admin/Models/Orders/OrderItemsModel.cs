namespace Grand.Web.Admin.Models.Orders;

public record OrderItemsModel(IList<OrderItemModel> Items, string OrderItemId);

public record OrderItemModel(string Id, double UnitPriceExclTaxValue, int Quantity);