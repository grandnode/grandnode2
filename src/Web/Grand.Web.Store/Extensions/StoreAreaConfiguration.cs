using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Reflection;

namespace Grand.Web.Store.Extensions;

/// <summary>
///     Tells whether the plugin a provider comes from ships a configuration screen for the Store area.
///     <para>
///         Provider configuration urls are relative (e.g. "../ShippingByWeight/Configure"), so the very
///         same value resolves to /Admin/... under the Admin area and to /Store/... under this one. A
///         plugin whose data is not store-scoped only has an [Area("Admin")] controller - linking a store
///         owner there produces a 404. The Store grids therefore ask this first and render plain text
///         instead of a dead link when the answer is no.
///     </para>
/// </summary>
public static class StoreAreaConfiguration
{
    //plugin assemblies never change within a process lifetime, so the reflection cost is paid once
    private static readonly ConcurrentDictionary<Assembly, bool> Cache = new();
    private static readonly ConcurrentDictionary<Assembly, string> UrlCache = new();

    /// <summary>
    ///     Whether the provider's plugin exposes at least one controller in the Store area.
    /// </summary>
    public static bool Exists(object provider)
    {
        return provider != null && Cache.GetOrAdd(provider.GetType().Assembly, HasStoreAreaController);
    }

    /// <summary>
    ///     The url of the provider's configuration screen within the Store area, or null when its
    ///     plugin ships none.
    ///     <para>
    ///         A provider's own <c>ConfigurationUrl</c> cannot be reused here: some plugins declare it
    ///         relative ("../ShippingByWeight/Configure", which resolves per area) but others declare it
    ///         absolute ("/Admin/PaymentBrainTree/Configure"), which would send a store manager into the
    ///         Admin area. The url is therefore built from the Store-area controller the plugin actually
    ///         ships, which by convention exposes a Configure action.
    ///     </para>
    /// </summary>
    public static string GetConfigurationUrl(object provider)
    {
        if (provider == null) return null;
        var controller = UrlCache.GetOrAdd(provider.GetType().Assembly, FindStoreAreaConfigurationController);
        return controller == null ? null : $"/{Constants.AreaStore}/{controller}/Configure";
    }

    private static bool HasStoreAreaController(Assembly assembly)
    {
        return GetLoadableTypes(assembly).Any(IsStoreAreaController);
    }

    private static string FindStoreAreaConfigurationController(Assembly assembly)
    {
        //a configuration screen has a GET and a POST Configure, so the overloads are enumerated
        //rather than asked for by name - GetMethod would throw on the ambiguity
        var controller = GetLoadableTypes(assembly).FirstOrDefault(type =>
            IsStoreAreaController(type) &&
            type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Any(method => method.Name == "Configure"));

        return controller == null ? null : controller.Name.Replace("Controller", string.Empty);
    }

    private static bool IsStoreAreaController(Type type)
    {
        return typeof(ControllerBase).IsAssignableFrom(type) &&
               string.Equals(type.GetCustomAttribute<AreaAttribute>()?.RouteValue, Constants.AreaStore,
                   StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     A plugin assembly can reference types that fail to load; those types are simply not controllers
    ///     we could route to, so they are skipped rather than failing the whole provider list.
    /// </summary>
    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type != null);
        }
    }
}
