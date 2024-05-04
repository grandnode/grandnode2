using Grand.SharedKernel.Attributes;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace Grand.Api.Infrastructure.Filters;

public class IgnoreFieldFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var type = context.Type;
        if (!schema.Properties.Any() || type == null) return;

        var excludedPropertyNames = type
            .GetProperties()
            .Where(
                t => t.GetCustomAttribute<IgnoreApiAttribute>() != null
            ).Select(d => d.Name).ToList();

        if (!excludedPropertyNames.Any()) return;

        var excludedSchemaPropertyKey = schema.Properties
            .Where(
                ap => excludedPropertyNames.Any(
                    pn => pn.Equals(ap.Key, StringComparison.InvariantCultureIgnoreCase)
                )
            ).Select(ap => ap.Key);

        foreach (var propertyToExclude in excludedSchemaPropertyKey) schema.Properties.Remove(propertyToExclude);
    }
}