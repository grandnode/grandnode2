using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Grand.Api.Extensions;

public class SwaggerODataControllerDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // remove controller
        foreach (var apiDescription in context.ApiDescriptions)
        {
            var actionDescriptor = (ControllerActionDescriptor)apiDescription.ActionDescriptor;
            if (actionDescriptor.ControllerName == "Metadata")
                swaggerDoc.Paths.Remove($"/{apiDescription.RelativePath}");
        }

        // remove schemas
        foreach (var (key, _) in swaggerDoc.Components.Schemas)
            if (key.Contains("Edm") || key.Contains("OData"))
                swaggerDoc.Components.Schemas.Remove(key);
    }
}