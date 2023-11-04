using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Grand.Web.Vendor.Extensions;

public static class ModelStateExtensions
{
    public static string GetErrors(this ModelStateDictionary modelState)
    {
        var errors = modelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

        return string.Join(Environment.NewLine, errors);
    }
}