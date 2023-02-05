using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Grand.Api.Extensions
{
    public class AddParamOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.ApiDescription.GroupName == "v1" && context.ApiDescription.HttpMethod == "GET" && context.ApiDescription.ParameterDescriptions.Count == 0)
            {
                ApplyV1(operation, context);
            }
        }

        private void ApplyV1(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Parameters.Add(new OpenApiParameter() {
                    Name = "$top",
                    AllowReserved = true,
                    In = ParameterLocation.Query,
                    Description = "Show only the first n items.",
                    Required = false,
                    Schema = new OpenApiSchema() {
                        Minimum = 0,
                        Type = "integer"
                    }
                });
                operation.Parameters.Add(new OpenApiParameter() {
                    Name = "$skip",
                    AllowReserved = true,
                    In = ParameterLocation.Query,
                    Description = "Skip the first n items",
                    Required = false,
                    Schema = new OpenApiSchema() {
                        Minimum = 0,
                        Type = "integer"
                    }
                });
                operation.Parameters.Add(new OpenApiParameter() {
                    Name = "$count",
                    AllowReserved = true,
                    In = ParameterLocation.Query,
                    Description = "Include count of items",
                    Required = false,
                    Schema = new OpenApiSchema() {
                        Type = "boolean"
                    }
                });
                operation.Parameters.Add(new OpenApiParameter() {
                    Name = "$orderby",
                    AllowReserved = true,
                    In = ParameterLocation.Query,
                    Description = "Order items by property values",
                    Required = false,
                    Schema = new OpenApiSchema() {
                        Type = "string"
                    }
                });
                operation.Parameters.Add(new OpenApiParameter() {
                    Name = "$filter",
                    AllowReserved = true,
                    In = ParameterLocation.Query,
                    Description = "Filter items by property values",
                    Required = false,
                    Schema = new OpenApiSchema() {
                        Type = "string"
                    }
                });
        }
    }
}
