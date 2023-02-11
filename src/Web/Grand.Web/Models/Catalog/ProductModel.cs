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
    public bool LoadPicture { get; set; }
    [ModelBinder(BinderType = typeof(CustomAttributesBinder))]
    public IList<CustomAttributeModel> Attributes { get; set; }
}