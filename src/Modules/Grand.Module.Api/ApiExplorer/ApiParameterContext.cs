// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//https://github.com/dotnet/aspnetcore/blob/main/src/Mvc/Mvc.ApiExplorer/src/ApiParameterContext.cs

using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing.Template;

namespace Grand.Module.Api.ApiExplorer;

internal sealed class ApiParameterContext
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