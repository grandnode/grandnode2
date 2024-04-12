using Grand.Infrastructure.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Grand.Infrastructure.Extensions;

public static class MvcOptionsExtensions
{
    public static void UseJsonBodyModelBinderProviderInsteadOf<T>(this MvcOptions mvcOptions)
        where T : IModelBinderProvider
    {
        var replacedModelBinderProvider = mvcOptions.ModelBinderProviders.OfType<T>().FirstOrDefault();
        if (replacedModelBinderProvider == null) return;
        var index = mvcOptions.ModelBinderProviders.IndexOf(replacedModelBinderProvider);
        var customProvider = new JsonBodyModelBinderProvider(replacedModelBinderProvider);
        mvcOptions.ModelBinderProviders.Remove(replacedModelBinderProvider);
        mvcOptions.ModelBinderProviders.Insert(index, customProvider);
    }
}