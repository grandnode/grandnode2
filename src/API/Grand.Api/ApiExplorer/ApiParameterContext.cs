//https://github.com/dotnet/aspnetcore/blob/main/src/Mvc/Mvc.ApiExplorer/src/ApiParameterContext.cs

using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing.Template;

namespace Grand.Api.ApiExplorer;

public class ApiParameterContext
{
    public ApiParameterContext(
        IModelMetadataProvider metadataProvider,
        ControllerActionDescriptor actionDescriptor,
        IReadOnlyList<TemplatePart> routeParameters)
    {
        MetadataProvider = metadataProvider;
        ActionDescriptor = actionDescriptor;
        RouteParameters = routeParameters;

        Results = new List<ApiParameterDescription>();
    }

    public ControllerActionDescriptor ActionDescriptor { get; }

    public IModelMetadataProvider MetadataProvider { get; }

    public IList<ApiParameterDescription> Results { get; }

    public IReadOnlyList<TemplatePart> RouteParameters { get; }
}