using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Grand.Web.Common.Binders;

public class CustomAttributesBinderProvider : IModelBinderProvider
{
    public IModelBinder GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.Metadata.ModelType == typeof(IList<CustomAttributeModel>))
        {
            return new BinderTypeModelBinder(typeof(CustomAttributesBinder));
        }

        return null;
    }
}
