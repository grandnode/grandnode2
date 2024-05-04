using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Grand.Api.Extensions;

public class AddParamOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        switch (context.ApiDescription.GroupName)
        {
            case "v1" when context.ApiDescription.HttpMethod == "GET" &&
                           context.ApiDescription.ParameterDescriptions.Count == 0:
                ApplyV1(operation);
                break;
            case "v2" when context.ApiDescription.HttpMethod == "POST":
                ApplyV2(operation, context);
                break;
        }
    }

    private void ApplyV1(OpenApiOperation operation)
    {
        operation.Parameters.Add(new OpenApiParameter {
            Name = "$top",
            AllowReserved = true,
            In = ParameterLocation.Query,
            Description = "Show only the first n items.",
            Required = false,
            Schema = new OpenApiSchema {
                Minimum = 0,
                Type = "integer"
            }
        });
        operation.Parameters.Add(new OpenApiParameter {
            Name = "$skip",
            AllowReserved = true,
            In = ParameterLocation.Query,
            Description = "Skip the first n items",
            Required = false,
            Schema = new OpenApiSchema {
                Minimum = 0,
                Type = "integer"
            }
        });
        operation.Parameters.Add(new OpenApiParameter {
            Name = "$count",
            AllowReserved = true,
            In = ParameterLocation.Query,
            Description = "Include count of items",
            Required = false,
            Schema = new OpenApiSchema {
                Type = "boolean"
            }
        });
        operation.Parameters.Add(new OpenApiParameter {
            Name = "$orderby",
            AllowReserved = true,
            In = ParameterLocation.Query,
            Description = "Order items by property values",
            Required = false,
            Schema = new OpenApiSchema {
                Type = "string"
            }
        });
        operation.Parameters.Add(new OpenApiParameter {
            Name = "$filter",
            AllowReserved = true,
            In = ParameterLocation.Query,
            Description = "Filter items by property values",
            Required = false,
            Schema = new OpenApiSchema {
                Type = "string"
            }
        });
        operation.Parameters.Add(new OpenApiParameter {
            Name = "$select",
            AllowReserved = true,
            In = ParameterLocation.Query,
            Description = "Select specific properties from the model",
            Required = false,
            Schema = new OpenApiSchema {
                Type = "string"
            }
        });
    }

    private void ApplyV2(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        if (context.MethodInfo.GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), true).Any())
            operation.Parameters.Add(new OpenApiParameter {
                Name = "X-CSRF-TOKEN",
                In = ParameterLocation.Header,
                Required = true,
                Schema = new OpenApiSchema {
                    Type = "string",
                    Description = "Antiforgery token"
                }
            });
    }
}