//https://github.com/dotnet/aspnetcore/blob/main/src/Mvc/Mvc.ApiExplorer/src/DefaultApiDescriptionProvider.cs

#nullable enable

using Grand.SharedKernel.Attributes;
using Grand.SharedKernel.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Options;
using System.Reflection;
using ApiControllerAttribute = Grand.SharedKernel.Attributes.ApiControllerAttribute;

namespace Grand.Api.ApiExplorer;

public class MetadataApiDescriptionProvider : IApiDescriptionProvider
{
    private readonly IInlineConstraintResolver _constraintResolver;
    private readonly IModelMetadataProvider _modelMetadataProvider;
    private readonly MvcOptions _mvcOptions;
    private readonly ApiResponseTypeProvider _responseTypeProvider;
    private readonly RouteOptions _routeOptions;

    /// <summary>
    ///     Creates a new instance of <see cref="DefaultApiDescriptionProvider" />.
    /// </summary>
    /// <param name="optionsAccessor">The accessor for <see cref="MvcOptions" />.</param>
    /// <param name="constraintResolver">
    ///     The <see cref="IInlineConstraintResolver" /> used for resolving inline
    ///     constraints.
    /// </param>
    /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider" />.</param>
    /// <param name="mapper">The <see cref="IActionResultTypeMapper" />.</param>
    /// <param name="routeOptions">The accessor for <see cref="RouteOptions" />.</param>
    /// <remarks>The <paramref name="mapper" /> parameter is currently ignored.</remarks>
    public MetadataApiDescriptionProvider(
        IOptions<MvcOptions> optionsAccessor,
        IInlineConstraintResolver constraintResolver,
        IModelMetadataProvider modelMetadataProvider,
        IActionResultTypeMapper mapper,
        IOptions<RouteOptions> routeOptions)
    {
        _mvcOptions = optionsAccessor.Value;
        _constraintResolver = constraintResolver;
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
            if (!action.ControllerTypeInfo.GetCustomAttributes<ApiControllerAttribute>(true)
                    .Any())
                continue;

            if (action.MethodInfo.GetCustomAttributes<IgnoreApiAttribute>(true).Any())
                continue;

            if (action.AttributeRouteInfo is { SuppressPathMatching: true }) continue;

            var httpMethods = GetHttpMethods(action);
            foreach (var httpMethod in httpMethods) context.Results.Add(CreateApiDescription(action, httpMethod, "v2"));
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
        var path = GetRelativePath(action, parsedTemplate);

        var apiDescription = new ApiDescription {
            ActionDescriptor = action,
            GroupName = groupName,
            HttpMethod = httpMethod,
            RelativePath = path
        };

        var templateParameters = parsedTemplate?.Parameters.ToList() ?? [];

        var parameterContext = new ApiParameterContext(_modelMetadataProvider, action, templateParameters);
        foreach (var parameter in GetParameters(parameterContext,
                     httpMethod.Equals("GET", StringComparison.CurrentCultureIgnoreCase)))
            apiDescription.ParameterDescriptions.Add(parameter);

        var requestMetadataAttributes = GetRequestMetadataAttributes(action);

        var apiResponseTypes = _responseTypeProvider.GetApiResponseTypes(action);
        foreach (var apiResponseType in apiResponseTypes) apiDescription.SupportedResponseTypes.Add(apiResponseType);

        // It would be possible here to configure an action with multiple body parameters, in which case you
        // could end up with duplicate data.
        if (apiDescription.ParameterDescriptions.Count > 0)
        {
            var contentTypes = GetDeclaredContentTypes(requestMetadataAttributes);
            foreach (var parameter in apiDescription.ParameterDescriptions)
                if (parameter.Source == BindingSource.Body ||
                    parameter.Source == BindingSource.Form)
                {
                    // For request body bound parameters, determine the content types supported
                    // by input formatters.
                    var requestFormats = GetSupportedFormats(contentTypes, parameter.Type);
                    foreach (var format in requestFormats) apiDescription.SupportedRequestFormats.Add(format);
                }
                else if (parameter.Source == BindingSource.FormFile)
                {
                    // Add all declared media types since FormFiles do not get processed by formatters.
                    foreach (var contentType in contentTypes)
                        apiDescription.SupportedRequestFormats.Add(new ApiRequestFormat {
                            MediaType = contentType
                        });
                }
        }

        return apiDescription;
    }

    private IList<ApiParameterDescription> GetParameters(ApiParameterContext context, bool httpGet)
    {
        // First, get parameters from the model-binding/parameter-binding side of the world.
        foreach (var actionParameter in context.ActionDescriptor.Parameters)
        {
            var visitor = new PseudoModelBindingVisitor(context, actionParameter);

            ModelMetadata metadata;
            if (actionParameter is ControllerParameterDescriptor controllerParameterDescriptor &&
                _modelMetadataProvider is ModelMetadataProvider provider)
                // The default model metadata provider derives from ModelMetadataProvider
                // and can therefore supply information about attributes applied to parameters.
                metadata = provider.GetMetadataForParameter(controllerParameterDescriptor.ParameterInfo);
            else
                // For backward compatibility, if there's a custom model metadata provider that
                // only implements the older IModelMetadataProvider interface, access the more
                // limited metadata information it supplies. In this scenario, validation attributes
                // are not supported on parameters.
                metadata = _modelMetadataProvider.GetMetadataForType(actionParameter.ParameterType);

            if (!httpGet &&
                actionParameter.BindingInfo == null &&
                !CommonHelper.IsSimpleType(actionParameter.ParameterType))
            {
                var bindingContext = new ApiParameterDescriptionContext(
                    metadata,
                    new BindingInfo {
                        BindingSource = new BindingSource("Body", "Body", true, true)
                    },
                    actionParameter.Name);
                visitor.WalkParameter(bindingContext);
            }
            else
            {
                var bindingContext = new ApiParameterDescriptionContext(
                    metadata,
                    actionParameter.BindingInfo,
                    actionParameter.Name);
                visitor.WalkParameter(bindingContext);
            }
        }

        foreach (var actionParameter in context.ActionDescriptor.BoundProperties)
        {
            var visitor = new PseudoModelBindingVisitor(context, actionParameter);
            var modelMetadata = context.MetadataProvider.GetMetadataForProperty(
                context.ActionDescriptor.ControllerTypeInfo.AsType(),
                actionParameter.Name);

            var bindingContext = new ApiParameterDescriptionContext(
                modelMetadata,
                actionParameter.BindingInfo,
                actionParameter.Name);

            visitor.WalkParameter(bindingContext);
        }

        for (var i = context.Results.Count - 1; i >= 0; i--)
            // Remove any 'hidden' parameters. These are things that can't come from user input,
            // so they aren't worth showing.
            if (!context.Results[i].Source.IsFromRequest)
            {
                context.Results.RemoveAt(i);
            }
            else
            {
                if (context.Results[i].ModelMetadata is DefaultModelMetadata metadata &&
                    metadata.Attributes.Attributes.OfType<IgnoreApiUrlAttribute>().Any())
                    context.Results.RemoveAt(i);
            }

        // Next, we want to join up any route parameters with those discovered from the action's parameters.
        // This will result us in creating a parameter representation for each route parameter that does not
        // have a mapping parameter or bound property.
        ProcessRouteParameters(context);

        // Set IsRequired=true
        ProcessIsRequired(context, _mvcOptions);

        // Set DefaultValue
        ProcessParameterDefaultValue(context);

        return context.Results;
    }

    private void ProcessRouteParameters(ApiParameterContext context)
    {
        var routeParameters = new Dictionary<string, ApiParameterRouteInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var routeParameter in context.RouteParameters)
            routeParameters.Add(routeParameter.Name!, CreateRouteInfo(routeParameter));

        foreach (var parameter in context.Results)
            if (parameter.Source == BindingSource.Path ||
                parameter.Source == BindingSource.ModelBinding ||
                parameter.Source == BindingSource.Custom)
                if (routeParameters.TryGetValue(parameter.Name, out var routeInfo))
                {
                    parameter.RouteInfo = routeInfo;
                    routeParameters.Remove(parameter.Name);

                    if (parameter.Source == BindingSource.ModelBinding &&
                        !parameter.RouteInfo.IsOptional)
                        // If we didn't see any information about the parameter, but we have
                        // a route parameter that matches, let's switch it to path.
                        parameter.Source = BindingSource.Path;
                }

        // Lastly, create a parameter representation for each route parameter that did not find
        // a partner.
        foreach (var routeParameter in routeParameters)
            context.Results.Add(new ApiParameterDescription {
                Name = routeParameter.Key,
                RouteInfo = routeParameter.Value,
                Source = BindingSource.Path
            });
    }

    internal static void ProcessIsRequired(ApiParameterContext context, MvcOptions mvcOptions)
    {
        foreach (var parameter in context.Results)
        {
            if (parameter.Source == BindingSource.Body)
            {
                if (parameter.BindingInfo == null ||
                    parameter.BindingInfo.EmptyBodyBehavior == EmptyBodyBehavior.Default)
                    parameter.IsRequired = !mvcOptions.AllowEmptyInputInBodyModelBinding;
                else
                    parameter.IsRequired = parameter.BindingInfo.EmptyBodyBehavior != EmptyBodyBehavior.Allow;
            }

            if (parameter.ModelMetadata is { IsBindingRequired: true }) parameter.IsRequired = true;

            if (parameter.Source == BindingSource.Path && parameter.RouteInfo is { IsOptional: false })
                parameter.IsRequired = true;
        }
    }

    internal static void ProcessParameterDefaultValue(ApiParameterContext context)
    {
        foreach (var parameter in context.Results)
            if (parameter.Source == BindingSource.Path)
                parameter.DefaultValue = parameter.RouteInfo?.DefaultValue;
        /*else
            {
                if (parameter.ParameterDescriptor is ControllerParameterDescriptor controllerParameter &&
                    ParameterDefaultValues.TryGetDeclaredParameterDefaultValue(controllerParameter.ParameterInfo,
                        out var defaultValue))
                {
                    parameter.DefaultValue = defaultValue;
                }
            }*/
    }

    private ApiParameterRouteInfo CreateRouteInfo(TemplatePart routeParameter)
    {
        var constraints = new List<IRouteConstraint>();
        foreach (var constraint in routeParameter.InlineConstraints)
            constraints.Add(_constraintResolver.ResolveConstraint(constraint.Constraint)!);

        return new ApiParameterRouteInfo {
            Constraints = constraints,
            DefaultValue = routeParameter.DefaultValue,
            IsOptional = routeParameter.IsOptional || routeParameter.DefaultValue != null
        };
    }

    private static IEnumerable<string> GetHttpMethods(ControllerActionDescriptor action)
    {
        return action.ActionConstraints is { Count: > 0 }
            ? action.ActionConstraints.OfType<HttpMethodActionConstraint>().SelectMany(c => c.HttpMethods)
            : new[] { string.Empty };
    }

    private static RouteTemplate? ParseTemplate(ControllerActionDescriptor action)
    {
        return action.AttributeRouteInfo?.Template != null
            ? TemplateParser.Parse(action.AttributeRouteInfo.Template)
            : null;
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

    private IReadOnlyList<ApiRequestFormat> GetSupportedFormats(MediaTypeCollection contentTypes, Type type)
    {
        if (contentTypes.Count == 0) contentTypes = ["application/json"];

        var results = new List<ApiRequestFormat>();
        foreach (var contentType in contentTypes)
        foreach (var formatter in _mvcOptions.InputFormatters.OfType<SystemTextJsonInputFormatter>())
            if (formatter is IApiRequestFormatMetadataProvider requestFormatMetadataProvider)
            {
                var supportedTypes = requestFormatMetadataProvider.GetSupportedContentTypes(contentType, type);

                if (supportedTypes != null)
                    foreach (var supportedType in supportedTypes.Where(x => x == "application/json"))
                        results.Add(new ApiRequestFormat {
                            Formatter = formatter,
                            MediaType = supportedType
                        });
            }

        return results;
    }

    internal static MediaTypeCollection GetDeclaredContentTypes(
        IReadOnlyList<IApiRequestMetadataProvider>? requestMetadataAttributes)
    {
        // Walk through all 'filter' attributes in order, and allow each one to see or override
        // the results of the previous ones. This is similar to the execution path for content-negotiation.
        var contentTypes = new MediaTypeCollection();
        if (requestMetadataAttributes != null)
            foreach (var metadataAttribute in requestMetadataAttributes)
                metadataAttribute.SetContentTypes(contentTypes);

        return contentTypes;
    }

    private static IApiRequestMetadataProvider[]? GetRequestMetadataAttributes(ControllerActionDescriptor action)
    {
        // This technique for enumerating filters will intentionally ignore any filter that is an IFilterFactory
        // while searching for a filter that implements IApiRequestMetadataProvider.
        //
        // The workaround for that is to implement the metadata interface on the IFilterFactory.
        return action.FilterDescriptors
            .Select(fd => fd.Filter)
            .OfType<IApiRequestMetadataProvider>()
            .ToArray();
    }

    private class ApiParameterDescriptionContext
    {
        public ApiParameterDescriptionContext(
            ModelMetadata metadata,
            BindingInfo? bindingInfo,
            string? propertyName)
        {
            // BindingMetadata can be null if the metadata represents properties.
            ModelMetadata = metadata;
            BinderModelName = bindingInfo?.BinderModelName;
            BindingSource = bindingInfo?.BindingSource;
            PropertyName = propertyName ?? metadata.Name;
            BindingInfo = bindingInfo;
        }

        public ModelMetadata ModelMetadata { get; }

        public string? BinderModelName { get; }

        public BindingSource? BindingSource { get; }

        public string? PropertyName { get; }

        public BindingInfo? BindingInfo { get; }
    }

    private class PseudoModelBindingVisitor
    {
        public PseudoModelBindingVisitor(ApiParameterContext context, ParameterDescriptor parameter)
        {
            Context = context;
            Parameter = parameter;

            Visited = new HashSet<PropertyKey>(new PropertyKeyEqualityComparer());
        }

        public ApiParameterContext Context { get; }

        public ParameterDescriptor Parameter { get; }

        // Avoid infinite recursion by tracking properties.
        private HashSet<PropertyKey> Visited { get; }

        public void WalkParameter(ApiParameterDescriptionContext context)
        {
            // Attempt to find a binding source for the parameter
            //
            // The default is ModelBinding (aka all default value providers)
            var source = BindingSource.ModelBinding;
            Visit(context, source, string.Empty);
        }

        private void Visit(
            ApiParameterDescriptionContext bindingContext,
            BindingSource ambientSource,
            string containerName)
        {
            var source = bindingContext.BindingSource;
            if (source != null && source.IsGreedy)
            {
                // We have a definite answer for this model. This is a greedy source like
                // [FromBody] so there's no need to consider properties.
                Context.Results.Add(CreateResult(bindingContext, source, containerName));
                return;
            }

            var modelMetadata = bindingContext.ModelMetadata;

            // For any property which is a leaf node, we don't want to keep traversing:
            //
            //  1)  Collections - while it's possible to have binder attributes on the inside of a collection,
            //      it hardly seems useful, and would result in some very weird binding.
            //
            //  2)  Simple Types - These are generally part of the .net framework - primitives, or types which have a
            //      type converter from string.
            //
            //  3)  Types with no properties. Obviously nothing to explore there.
            //
            if (modelMetadata.IsEnumerableType ||
                !modelMetadata.IsComplexType ||
                modelMetadata.Properties.Count == 0)
            {
                Context.Results.Add(CreateResult(bindingContext, source ?? ambientSource, containerName));
                return;
            }

            // This will come from composite model binding - so investigate what's going on with each property.
            //
            // Ex:
            //
            //      public IActionResult PlaceOrder(OrderDTO order) {...}
            //
            //      public class OrderDTO
            //      {
            //          public int AccountId { get; set; }
            //
            //          [FromBody]
            //          public Order { get; set; }
            //      }
            //
            // This should result in two parameters:
            //
            //  AccountId - source: Any
            //  Order - source: Body
            //

            // We don't want to append the **parameter** name when building a model name.
            var newContainerName = containerName;
            if (modelMetadata.ContainerType != null) newContainerName = GetName(containerName, bindingContext);

            var metadataProperties = modelMetadata.Properties;
            var metadataPropertiesCount = metadataProperties.Count;
            for (var i = 0; i < metadataPropertiesCount; i++)
            {
                var propertyMetadata = metadataProperties[i];
                var key = new PropertyKey(propertyMetadata, source);
                var bindingInfo = BindingInfo.GetBindingInfo(Enumerable.Empty<object>(), propertyMetadata);

                var propertyContext = new ApiParameterDescriptionContext(
                    propertyMetadata,
                    bindingInfo,
                    null);

                if (Visited.Add(key))
                {
                    Visit(propertyContext, source ?? ambientSource, newContainerName);
                    Visited.Remove(key);
                }
                else
                {
                    // This is cycle, so just add a result rather than traversing.
                    Context.Results.Add(CreateResult(propertyContext, source ?? ambientSource, newContainerName));
                }
            }
        }

        private ApiParameterDescription CreateResult(
            ApiParameterDescriptionContext bindingContext,
            BindingSource source,
            string containerName)
        {
            return new ApiParameterDescription {
                ModelMetadata = bindingContext.ModelMetadata,
                Name = GetName(containerName, bindingContext),
                Source = source,
                Type = bindingContext.ModelMetadata.ModelType,
                ParameterDescriptor = Parameter,
                BindingInfo = bindingContext.BindingInfo
            };
        }

        private static string GetName(string containerName, ApiParameterDescriptionContext metadata)
        {
            var propertyName = !string.IsNullOrEmpty(metadata.BinderModelName)
                ? metadata.BinderModelName
                : metadata.PropertyName;
            return ModelNames.CreatePropertyModelName(containerName, propertyName);
        }

        private readonly struct PropertyKey
        {
            public readonly Type ContainerType;

            public readonly string PropertyName;

            public readonly BindingSource? Source;

            public PropertyKey(ModelMetadata metadata, BindingSource? source)
            {
                ContainerType = metadata.ContainerType!;
                PropertyName = metadata.PropertyName!;
                Source = source;
            }
        }

        private class PropertyKeyEqualityComparer : IEqualityComparer<PropertyKey>
        {
            public bool Equals(PropertyKey x, PropertyKey y)
            {
                return
                    x.ContainerType == y.ContainerType &&
                    x.PropertyName == y.PropertyName &&
                    x.Source == y.Source;
            }

            public int GetHashCode(PropertyKey obj)
            {
                var hashCodeCombiner = new HashCode();
                hashCodeCombiner.Add(obj.ContainerType);
                hashCodeCombiner.Add(obj.PropertyName);
                hashCodeCombiner.Add(obj.Source);
                return hashCodeCombiner.ToHashCode();
            }
        }
    }
}