using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Grand.Api.Infrastructure
{
    public class EnumSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type.IsEnum)
            {
                var enumValues = schema.Enum.ToArray();
                foreach (var item in enumValues)
                {
                    var value = (OpenApiPrimitive<int>)item;
                    var name = Enum.GetName(context.Type, value.Value);
                    schema.Description += $"{value.Value} - {name}; ";
                }
            }
        }
    }
}

