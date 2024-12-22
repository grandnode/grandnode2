using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Models.ShoppingCart;

public class CheckoutAttributeSelectedModel
{
    [ModelBinder(BinderType = typeof(CustomAttributesBinder))]
    public IList<CustomAttributeModel> Attributes { get; set; } = new List<CustomAttributeModel>();
}