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

    /// <summary>
    ///     Whether the provider's plugin exposes at least one controller in the Store area.
    /// </summary>
    public static bool Exists(object provider)
    {
        return provider != null && Cache.GetOrAdd(provider.GetType().Assembly, HasStoreAreaController);
    }

    private static bool HasStoreAreaController(Assembly assembly)
    {
        return GetLoadableTypes(assembly).Any(type =>
            typeof(ControllerBase).IsAssignableFrom(type) &&
            string.Equals(type.GetCustomAttribute<AreaAttribute>()?.RouteValue, Constants.AreaStore,
                StringComparison.OrdinalIgnoreCase));
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
