using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Grand.Infrastructure.ModelBinding;

public class JsonBodyModelBinderProvider : IModelBinderProvider
{
    private readonly IModelBinderProvider _overriddenModelBinderProvider;

    public JsonBodyModelBinderProvider(IModelBinderProvider overriddenModelBinderProvider)
    {
        _overriddenModelBinderProvider = overriddenModelBinderProvider;
    }

    public IModelBinder GetBinder(ModelBinderProviderContext context)
    {
        var modelBinder = _overriddenModelBinderProvider.GetBinder(context);
        return modelBinder == null ? null : new JsonBodyModelBinder(modelBinder);
    }
}