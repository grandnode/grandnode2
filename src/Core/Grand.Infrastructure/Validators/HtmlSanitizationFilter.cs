using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using Grand.Infrastructure.Security;
using Grand.SharedKernel.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Grand.Infrastructure.Validators;

/// <summary>
///     Sanitizes bound models before the action body runs, for every property marked
///     <see cref="SanitizeHtmlAttribute" /> or <see cref="NoHtmlAttribute" />.
///     This is a filter rather than a validation attribute on purpose. A validation attribute cannot change the
///     value, only report it, which leaves the write to be blocked by a ModelState check the controller has to
///     remember to make - and leaves the API surface, which binds the same models, unprotected.
/// </summary>
public class HtmlSanitizationFilter : IAsyncActionFilter
{
    /// <summary>
    ///     Guards against a model graph that references itself through a chain of distinct instances, which the
    ///     visited set alone would not stop.
    /// </summary>
    private const int MaxDepth = 5;

    private static readonly ConcurrentDictionary<Type, SanitizationPlan> Plans = new();

    private readonly IHtmlSanitizationService _htmlSanitizationService;

    public HtmlSanitizationFilter(IHtmlSanitizationService htmlSanitizationService)
    {
        _htmlSanitizationService = htmlSanitizationService;
    }

    /// <summary>
    ///     Called before the action executes, after model binding is complete
    /// </summary>
    /// <param name="context">A context for action filters</param>
    /// <param name="next"></param>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        //nothing is bound from the body on a safe method
        if (HttpMethods.IsGet(context.HttpContext.Request.Method) ||
            HttpMethods.IsHead(context.HttpContext.Request.Method))
        {
            await next();
            return;
        }

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var argument in context.ActionArguments.Values)
            Sanitize(argument, visited, 0);

        await next();
    }

    private void Sanitize(object model, HashSet<object> visited, int depth)
    {
        if (model is null || depth > MaxDepth) return;

        if (model is IEnumerable collection and not string)
        {
            foreach (var item in collection)
                Sanitize(item, visited, depth + 1);
            return;
        }

        var type = model.GetType();
        if (CommonHelper.IsSimpleType(type)) return;
        if (!visited.Add(model)) return;

        var plan = Plans.GetOrAdd(type, BuildPlan);

        foreach (var property in plan.RichText)
            Rewrite(model, property, _htmlSanitizationService.SanitizeRichText);

        foreach (var property in plan.PlainText)
            Rewrite(model, property, _htmlSanitizationService.StripHtml);

        foreach (var property in plan.Nested)
            Sanitize(property.GetValue(model), visited, depth + 1);
    }

    private static void Rewrite(object model, PropertyInfo property, Func<string, string> sanitize)
    {
        if (property.GetValue(model) is not string value) return;

        var sanitized = sanitize(value);
        if (!string.Equals(sanitized, value, StringComparison.Ordinal))
            property.SetValue(model, sanitized);
    }

    private static SanitizationPlan BuildPlan(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.CanRead && x.GetIndexParameters().Length == 0)
            .ToArray();

        var richText = properties
            .Where(x => x.PropertyType == typeof(string) && x.CanWrite &&
                        x.IsDefined(typeof(SanitizeHtmlAttribute), true))
            .ToArray();

        var plainText = properties
            .Where(x => x.PropertyType == typeof(string) && x.CanWrite &&
                        x.IsDefined(typeof(NoHtmlAttribute), true))
            .ToArray();

        //localized models hang off a Locales collection, so the marked properties are one level down; only
        //descend into types this repository owns, to keep the walk away from framework object graphs
        var nested = properties
            .Where(x => x.PropertyType != typeof(string) && !CommonHelper.IsSimpleType(x.PropertyType))
            .Where(IsOwnedType)
            .ToArray();

        return new SanitizationPlan(richText, plainText, nested);
    }

    private static bool IsOwnedType(PropertyInfo property)
    {
        var type = property.PropertyType;

        if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            var elementType = type.IsArray
                ? type.GetElementType()
                : type.IsGenericType
                    ? type.GetGenericArguments().FirstOrDefault()
                    : null;

            if (elementType is null || CommonHelper.IsSimpleType(elementType)) return false;
            type = elementType;
        }

        return type.Assembly.GetName().Name?.StartsWith("Grand", StringComparison.Ordinal) == true;
    }

    private record SanitizationPlan(PropertyInfo[] RichText, PropertyInfo[] PlainText, PropertyInfo[] Nested);
}
