using Grand.Module.Api.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Grand.Module.Api.Infrastructure.Extensions
{
    public static class OpenApiOptionsExtensions
    {
        public static void AddOperationTransformer(this OpenApiOptions options)
        {
            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any())
                {
                    operation.Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Scheme = "Bearer" }] = new List<string>()
                        }
                    };
                }
                var enableQuery = context.Description.ActionDescriptor?.FilterDescriptors.Where(x => x.Filter.GetType() == typeof(EnableQueryAttribute)).FirstOrDefault();
                if (enableQuery != null)
                {
                    if (operation.Parameters == null)
                    {
                        operation.Parameters = new List<OpenApiParameter>();
                    }

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
                return Task.CompletedTask;
            });
        }

        public static void AddContactDocumentTransformer(this OpenApiOptions options, string name)
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info.Contact = new OpenApiContact {
                    Name = name,
                    Email = "support@grandnode.com",
                    Url = new Uri("https://grandnode.com")
                };
                document.Info.License = new OpenApiLicense {
                    Name = "GNU General Public License v3.0",
                    Url = new Uri("https://github.com/grandnode/grandnode2/blob/main/LICENSE")
                };
                return Task.CompletedTask;
            });
        }
    }
}
