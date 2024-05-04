using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Grand.Web.Common.Extensions;

public static class ModelStateExtensions
{
    public static object SerializeErrors(this ModelStateDictionary modelStateDictionary)
    {
        return modelStateDictionary.Where(entry => entry.Value != null && entry.Value.Errors.Any())
            .ToDictionary(entry => entry.Key, entry => SerializeModelState(entry.Value));
    }

    private static Dictionary<string, object> SerializeModelState(ModelStateEntry modelState)
    {
        var dictionary = new Dictionary<string, object> {
            ["errors"] = modelState.Errors.Select(modelError => modelError.ErrorMessage)
                .Where(errorText => !string.IsNullOrEmpty(errorText)).ToArray()
        };
        return dictionary;
    }

    public static object ToDataSourceResult(this ModelStateDictionary modelState)
    {
        return !modelState.IsValid ? modelState.SerializeErrors() : null;
    }
}