using Grand.Web.Common.Binders;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Models.Orders;

public record AddProductToOrderModel(
    string OrderId,
    string ProductId,
    double UnitPriceInclTax,
    double UnitPriceExclTax,
    int Quantity,
    int TaxRate)
{
    public GiftvoucherModel Giftvoucher { get; set; }

    [ModelBinder(BinderType = typeof(CustomAttributesBinder))]
    public IList<CustomAttributeModel> SelectedAttributes { get; set; }
}

public record GiftvoucherModel(
    string RecipientName,
    string RecipientEmail,
    string SenderName,
    string SenderEmail,
    string Message);