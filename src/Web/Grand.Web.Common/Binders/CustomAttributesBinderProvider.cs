using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Grand.Web.Common.Binders;

public class CustomAttributesBinderProvider : IModelBinderProvider
{
    public IModelBinder GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Metadata.ModelType == typeof(IList<CustomAttributeModel>)
            ? new BinderTypeModelBinder(typeof(CustomAttributesBinder))
            : null;
    }
}