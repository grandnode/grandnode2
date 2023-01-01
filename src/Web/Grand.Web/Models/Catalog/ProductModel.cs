using Grand.Web.Common.Binders;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Models.Catalog;

public class ProductModel
{
    public string ProductId { get; set; }
    public string WarehouseId { get; set; }
    public string ReservationDatepickerFrom { get; set; }
    public string ReservationDatepickerTo { get; set; }
    public string RecipientName{ get; set; }
    public string RecipientEmail{ get; set; }
    public string SenderName{ get; set; }
    public string SenderEmail{ get; set; }
    public string Message{ get; set; }
    
    [ModelBinder(BinderType = typeof(CustomAttributesBinder))]
    public IList<CustomAttributeModel> Attributes { get; set; }
    public string Reservation { get; set; }
    public string CustomerEnteredPrice { get; set; }
    public int EnteredQuantity { get; set; }
}