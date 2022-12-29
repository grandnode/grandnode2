using Grand.Web.Common.Binders;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Models.Common;
public class ContactAttributeChangeModel
{
    public ContactAttributeChangeModel()
    {
        Attributes = new List<CustomAttributeModel>();
    }
    [ModelBinder(BinderType = typeof(CustomAttributesBinder))]
    public IList<CustomAttributeModel> Attributes { get; set; }
}