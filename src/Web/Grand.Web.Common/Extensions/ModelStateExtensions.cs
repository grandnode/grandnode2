using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Grand.Web.Common.Extensions;

public static class ModelStateExtensions
{
    public static object SerializeErrors(this ModelStateDictionary modelStateDictionary)
    {
        var errors = new Dictionary<string, Dictionary<string, object>>();
        foreach (var (key, entry) in modelStateDictionary)
            if (entry is { Errors.Count: > 0 })
                errors[key] = SerializeModelState(entry);
        return errors;
    }

    private static Dictionary<string, object> SerializeModelState(ModelStateEntry modelState)
    {
        var dictionary = new Dictionary<string, object> {
            ["errors"] = modelState.Errors.Select(modelError => modelError.ErrorMessage)
                .Where(errorText => !string.IsNullOrEmpty(errorText)).ToArray()
        };
        return dictionary;
    }

    public static object? ToDataSourceResult(this ModelStateDictionary modelState)
    {
        return !modelState.IsValid ? modelState.SerializeErrors() : null;
    }
}