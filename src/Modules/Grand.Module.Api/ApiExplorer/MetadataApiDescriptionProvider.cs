// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//https://github.com/dotnet/aspnetcore/blob/main/src/Mvc/Mvc.ApiExplorer/src/DefaultApiDescriptionProvider.cs

#nullable enable

using Grand.SharedKernel.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Grand.Module.Api.ApiExplorer;

public class MetadataApiDescriptionProvider : IApiDescriptionProvider
{
    private readonly MvcOptions _mvcOptions;
    private readonly ApiResponseTypeProvider _responseTypeProvider;
    private readonly RouteOptions _routeOptions;
    private readonly IModelMetadataProvider _modelMetadataProvider;

    public MetadataApiDescriptionProvider(
        IOptions<MvcOptions> optionsAccessor,
        IModelMetadataProvider modelMetadataProvider,
        IActionResultTypeMapper mapper,
        IOptions<RouteOptions> routeOptions)
    {
        _mvcOptions = optionsAccessor.Value;
        _modelMetadataProvider = modelMetadataProvider;
        _responseTypeProvider = new ApiResponseTypeProvider(modelMetadataProvider, mapper, _mvcOptions);
        _routeOptions = routeOptions.Value;
    }

    /// <inheritdoc />
    public int Order => -1000;

    /// <inheritdoc />
    public void OnProvidersExecuting(ApiDescriptionProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var action in context.Actions.OfType<ControllerActionDescriptor>())
        {
            var apiGroup = action.ControllerTypeInfo.GetCustomAttributes<ApiGroupAttribute>().FirstOrDefault();
            if (apiGroup == null) continue;

            if (action.MethodInfo.GetCustomAttributes<IgnoreApiAttribute>(true).Any())
                continue;

            if (action.AttributeRouteInfo is { SuppressPathMatching: true }) continue;

            var httpMethods = GetHttpMethods(action);
            foreach (var httpMethod in httpMethods)
            {
                var apiDescription = CreateApiDescription(action, httpMethod, apiGroup.GroupName);
                context.Results.Add(apiDescription);
            };
        }
    }

    /// <inheritdoc />
    public void OnProvidersExecuted(ApiDescriptionProviderContext context)
    {
    }

    private ApiDescription CreateApiDescription(
        ControllerActionDescriptor action,
        string httpMethod,
        string groupName)
    {
        var parsedTemplate = ParseTemplate(action);

        var apiDescription = new ApiDescription {
            ActionDescriptor = action,
            GroupName = groupName,
            HttpMethod = httpMethod,
            RelativePath = GetRelativePath(action, parsedTemplate)
        };

        var apiResponseTypes = _responseTypeProvider.GetApiResponseTypes(action);
        foreach (var apiResponseType in apiResponseTypes)
        {
            apiDescription.SupportedResponseTypes.Add(apiResponseType);
        }

        if (httpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            AddGetParameters(apiDescription, action);
        }
        if (httpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            AddPostParameters(apiDescription, action);
        }
        return apiDescription;
    }
    private void AddGetParameters(ApiDescription apiDescription, ControllerActionDescriptor action)
    {
        var actionParameters = apiDescription.ActionDescriptor.Parameters;
        foreach (var param in actionParameters)
        {
            var paramType = param.ParameterType;
            if (!IsSimpleType(paramType))
            {
                var properties = paramType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    if (property.GetCustomAttribute<IgnoreApiAttribute>() != null)
                        continue;

                    apiDescription.ParameterDescriptions.Add(new ApiParameterDescription {
                        Name = property.Name,
                        ModelMetadata = _modelMetadataProvider.GetMetadataForType(property.PropertyType),
                        Source = BindingSource.Query,
                        Type = property.PropertyType
                    });
                }
            }
            else
                apiDescription.ParameterDescriptions.Add(new ApiParameterDescription {
                    Name = param.Name,
                    ModelMetadata = GetModel(param),
                    Source = BindingSource.Query,
                    Type = param.ParameterType
                });
        }
    }

    private void AddPostParameters(ApiDescription apiDescription, ControllerActionDescriptor action)
    {
        var actionParameters = apiDescription.ActionDescriptor.Parameters;
        foreach (var param in actionParameters)
        {
            apiDescription.ParameterDescriptions.Add(new ApiParameterDescription {
                Name = param.Name,
                ModelMetadata = GetModel(param),
                Source = BindingSource.Body,
                Type = param.ParameterType
            });
        }
    }
    private ModelMetadata GetModel(ParameterDescriptor param)
    {
        ModelMetadata metadata;
        if (param is ControllerParameterDescriptor controllerParameterDescriptor &&
            _modelMetadataProvider is ModelMetadataProvider provider)
        {
            metadata = provider.GetMetadataForParameter(controllerParameterDescriptor.ParameterInfo);
        }
        else
        {
            metadata = _modelMetadataProvider.GetMetadataForType(param.ParameterType);
        }

        return metadata;
    }
    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(Guid)
            || type == typeof(TimeSpan);
    }


    private static IEnumerable<string> GetHttpMethods(ControllerActionDescriptor action)
    {
        if (action.ActionConstraints != null && action.ActionConstraints.Count > 0)
        {
            return action.ActionConstraints.OfType<HttpMethodActionConstraint>().SelectMany(c => c.HttpMethods);
        }
        else
        {
            return [];
        }
    }

    private static RouteTemplate? ParseTemplate(ControllerActionDescriptor action)
    {
        if (action.AttributeRouteInfo?.Template != null)
        {
            return TemplateParser.Parse(action.AttributeRouteInfo.Template);
        }

        return null;
    }

    private string? GetRelativePath(ControllerActionDescriptor action, RouteTemplate? parsedTemplate)
    {
        if (parsedTemplate == null) return $"/{action.ControllerName}/{action.ActionName}";

        var segments = new List<string>();

        foreach (var segment in parsedTemplate.Segments)
        {
            var currentSegment = string.Empty;
            foreach (var part in segment.Parts)
                if (part.IsLiteral)
                    currentSegment += _routeOptions.LowercaseUrls ? part.Text!.ToLowerInvariant() : part.Text;
                else if (part.IsParameter) currentSegment += "{" + part.Name + "}";

            segments.Add(currentSegment);
        }

        return string.Join("/", segments);
    }
}