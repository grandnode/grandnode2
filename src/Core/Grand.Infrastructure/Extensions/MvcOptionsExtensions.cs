using Grand.Infrastructure.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Grand.Infrastructure.Extensions;

public static class MvcOptionsExtensions
{
    public static void UseJsonBodyModelBinderProviderInsteadOf<T>(this MvcOptions mvcOptions) where T : IModelBinderProvider
    {
        var replacedModelBinderProvider = mvcOptions.ModelBinderProviders.OfType<T>().FirstOrDefault();
        if (replacedModelBinderProvider == null) return;
        var pos = 0;
        for (var i = 0; i < mvcOptions.ModelBinderProviders.Count; i++)
        {
            if (mvcOptions.ModelBinderProviders[i] is T)
            {
                pos = i;
            }
        }
        var customProvider = new JsonBodyModelBinderProvider(replacedModelBinderProvider);
        mvcOptions.ModelBinderProviders.Remove(replacedModelBinderProvider);
        mvcOptions.ModelBinderProviders.Insert(pos, customProvider);
    }
}