using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.RegularExpressions;

namespace Grand.Web.Common.Binders;

public class CustomAttributesBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));

        if (bindingContext.HttpContext.Request.HasFormContentType)
        {
            var model = new List<CustomAttributeModel>();
            var formCollection = bindingContext.HttpContext.Request.Form;
            foreach (var form in formCollection)
                if (form.Key.StartsWith("attributes"))
                    model.Add(new CustomAttributeModel { Key = GetKey(form.Key), Value = form.Value });

            bindingContext.Result = ModelBindingResult.Success(model);
        }

        return Task.CompletedTask;
    }

    private static string GetKey(string key)
    {
        var regex = new Regex("\\[(?<Value>\\w+)\\]");
        string value = null;
        var match = regex.Match(key);
        if (match.Success) value = match.Groups["Value"].Value;

        return value;
    }
}