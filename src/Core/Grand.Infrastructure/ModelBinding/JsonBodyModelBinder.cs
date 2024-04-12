using Grand.Infrastructure.TypeConverters.JsonConverters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Grand.Infrastructure.ModelBinding;

public class JsonBodyModelBinder : IModelBinder
{
    private readonly IModelBinder _overriddenModelBinder;

    public JsonBodyModelBinder(IModelBinder overriddenModelBinder)
    {
        _overriddenModelBinder = overriddenModelBinder;
    }

    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var httpContext = bindingContext.HttpContext;
        var hasJsonContentType = httpContext.Request.ContentType?.Contains("application/json") ?? false;
        if (hasJsonContentType)
        {
            string jsonPayload;
            using (var streamReader = new StreamReader(httpContext.Request.Body))
            {
                jsonPayload = await streamReader.ReadToEndAsync();
            }

            var options = new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new StringConverter());
            bindingContext.Result =
                ModelBindingResult.Success(JsonSerializer.Deserialize(jsonPayload, bindingContext.ModelType, options));
        }
        else
        {
            await _overriddenModelBinder.BindModelAsync(bindingContext);
        }
    }
}