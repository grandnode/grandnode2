using Grand.Web.Common.Binders;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Models.ShoppingCart;

public class CheckoutAttributeSelectedModel
{
    public CheckoutAttributeSelectedModel()
    {
        Attributes = new List<CustomAttributeModel>();
    }
    [ModelBinder(BinderType = typeof(CustomAttributesBinder))]
    public IList<CustomAttributeModel> Attributes { get; set; }
}