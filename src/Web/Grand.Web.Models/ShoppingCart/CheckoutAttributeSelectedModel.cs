using Grand.Web.Common.Binders;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Models.ShoppingCart;

public class CheckoutAttributeSelectedModel
{
    [ModelBinder(BinderType = typeof(CustomAttributesBinder))]
    public IList<CustomAttributeModel> Attributes { get; set; } = new List<CustomAttributeModel>();
}