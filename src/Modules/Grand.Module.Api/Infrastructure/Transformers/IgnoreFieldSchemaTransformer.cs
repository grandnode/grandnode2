using Grand.SharedKernel.Attributes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace Grand.Module.Api.Infrastructure.Transformers;

public class IgnoreFieldSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {

        var type = context.JsonTypeInfo.Type;
        if (!schema.Properties.Any() || type == null) return Task.CompletedTask;

        var excludedPropertyNames = type
            .GetProperties()
            .Where(
                t => t.GetCustomAttribute<IgnoreApiAttribute>() != null
            ).Select(d => d.Name).ToList();

        if (!excludedPropertyNames.Any()) return Task.CompletedTask;

        var excludedSchemaPropertyKey = schema.Properties
            .Where(
                ap => excludedPropertyNames.Any(
                    pn => pn.Equals(ap.Key, StringComparison.InvariantCultureIgnoreCase)
                )
            ).Select(ap => ap.Key);

        foreach (var propertyToExclude in excludedSchemaPropertyKey) schema.Properties.Remove(propertyToExclude);

        return Task.CompletedTask;
        //throw new NotImplementedException();
    }
}
