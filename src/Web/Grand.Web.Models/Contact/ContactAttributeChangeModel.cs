using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Models.Contact;

public class ContactAttributeChangeModel
{
    [ModelBinder(BinderType = typeof(CustomAttributesBinder))]
    public IList<CustomAttributeModel> Attributes { get; set; } = new List<CustomAttributeModel>();
}