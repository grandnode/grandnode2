using Grand.Module.Api.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Grand.Module.Api.Infrastructure.Extensions
{
    public static class OpenApiOptionsExtensions
    {
        public static void AddOperationTransformer(this OpenApiOptions options)
        {
            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
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
                        Description = "Order items by property values (LINQ notation)",
                        Example = new OpenApiString("Name, DisplayOrder"),
                        Required = false,
                        Schema = new OpenApiSchema {
                            Type = "string"
                        }
                    });
                    operation.Parameters.Add(new OpenApiParameter {
                        Name = "$filter",
                        AllowReserved = true,
                        In = ParameterLocation.Query,
                        Description = "Filter items by property values (LINQ notation) ",
                        Example = new OpenApiString("Name == \"John\""),
                        Required = false,
                        Schema = new OpenApiSchema {
                            Type = "string"
                        }
                    });
                    operation.Parameters.Add(new OpenApiParameter {
                        Name = "$select",
                        AllowReserved = true,
                        In = ParameterLocation.Query,
                        Description = "Select specific properties from the model (LINQ notation)",
                        Example = new OpenApiString("Id, Name"),
                        Required = false,
                        Schema = new OpenApiSchema {
                            Type = "string"
                        }
                    });
                }
                return Task.CompletedTask;
            });
        }
        public static void AddCsrfTokenTransformer(this OpenApiOptions options)
        {
            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                operation.Parameters ??= [];

                var antiforgeryToken = context.Description.ActionDescriptor.EndpointMetadata.OfType<AutoValidateAntiforgeryTokenAttribute>().Any();
                if (antiforgeryToken)
                    operation.Parameters.Add(new OpenApiParameter {
                        Name = "X-CSRF-TOKEN",
                        In = ParameterLocation.Header,
                        Required = true,
                        Schema = new OpenApiSchema {
                            Type = "string",
                            Description = "Antiforgery token"
                        }
                    });
                return Task.CompletedTask;
            });
        }
        public static void AddContactDocumentTransformer(this OpenApiOptions options, string name, string version)
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {                
                document.Info = new OpenApiInfo {
                    Description = "Grandnode API",
                    Title = name,
                    Version = version,
                    Contact = new OpenApiContact {
                        Name = name,
                        Email = "support@grandnode.com",
                        Url = new Uri("https://grandnode.com")
                    },
                    License = new OpenApiLicense {
                        Name = "GNU General Public License v3.0",
                        Url = new Uri("https://github.com/grandnode/grandnode2/blob/main/LICENSE")
                    }
                };

                return Task.CompletedTask;
            });
        }
        public static void AddClearServerDocumentTransformer(this OpenApiOptions options)
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Servers.Clear();
                return Task.CompletedTask;
            });
        }
    }
}
