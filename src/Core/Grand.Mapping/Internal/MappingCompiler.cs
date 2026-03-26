using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Grand.Mapping.Internal;

internal static class MappingCompiler
{
    public static Action<TSource, TDest> Compile<TSource, TDest>(List<MemberConfig> configs)
    {
        var steps = new List<Action<TSource, TDest>>();

        var directConfigs = new Dictionary<string, MemberConfig>(StringComparer.Ordinal);
        foreach (var c in configs.Where(c => !c.IsPath))
            directConfigs[c.MemberName] = c;

        var srcProps = typeof(TSource)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name);

        foreach (var destProp in typeof(TDest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite))
        {
            if (directConfigs.TryGetValue(destProp.Name, out var mc))
            {
                if (mc.IsIgnored) continue;

                var dp = destProp;
                var cond = mc.ConditionExpression != null
                    ? (Func<TSource, bool>)mc.ConditionExpression.Compile()
                    : null;

                if (mc.MapFromExpression != null)
                {
                    var getter = ToObjectGetter<TSource>(mc.MapFromExpression);
                    steps.Add((src, dst) =>
                    {
                        if (cond != null && !cond(src)) return;
                        dp.SetValue(dst, ConvertValue(getter(src), dp.PropertyType));
                    });
                }
                else if (srcProps.TryGetValue(destProp.Name, out var sp))
                {
                    var srcProp = sp;
                    steps.Add((src, dst) =>
                    {
                        if (cond != null && !cond(src)) return;
                        dp.SetValue(dst, ConvertValue(srcProp.GetValue(src), dp.PropertyType));
                    });
                }
            }
            else if (srcProps.TryGetValue(destProp.Name, out var srcProp))
            {
                var dp = destProp;
                var sp = srcProp;
                steps.Add((src, dst) => dp.SetValue(dst, ConvertValue(sp.GetValue(src), dp.PropertyType)));
            }
        }

        // ForPath steps
        foreach (var pc in configs.Where(c => c.IsPath && !c.IsIgnored
            && c.MapFromExpression != null && c.DestinationPathExpression != null))
        {
            var setter = BuildPathSetter(pc.DestinationPathExpression!);
            if (setter == null) continue;

            var getter = ToObjectGetter<TSource>(pc.MapFromExpression!);
            var cond = pc.ConditionExpression != null
                ? (Func<TSource, bool>)pc.ConditionExpression.Compile()
                : null;

            steps.Add((src, dst) =>
            {
                if (cond != null && !cond(src)) return;
                setter(dst, getter(src));
            });
        }

        return (src, dst) => { foreach (var s in steps) s(src, dst); };
    }

    // Compiles Expression<Func<TSource, TValue>> → Func<TSource, object?> to avoid DynamicInvoke.
    private static Func<TSource, object?> ToObjectGetter<TSource>(LambdaExpression expr)
    {
        var param = Expression.Parameter(typeof(TSource), "s");
        var body = Expression.Convert(Expression.Invoke(expr, param), typeof(object));
        return Expression.Lambda<Func<TSource, object?>>(body, param).Compile();
    }

    private static Action<object, object?>? BuildPathSetter(LambdaExpression pathExpr)
    {
        var members = new List<PropertyInfo>();
        var node = pathExpr.Body;
        while (node is MemberExpression me && me.Member is PropertyInfo pi)
        {
            members.Insert(0, pi);
            node = me.Expression!;
        }
        if (members.Count == 0) return null;
        var finalProp = members[^1];
        if (!finalProp.CanWrite) return null;
        var navigators = members.Take(members.Count - 1).ToArray();

        return (dst, val) =>
        {
            object? target = dst;
            foreach (var nav in navigators)
            {
                target = nav.GetValue(target);
                if (target == null) return;
            }
            finalProp.SetValue(target, ConvertValue(val, finalProp.PropertyType));
        };
    }

    // Runtime type coercion — mirrors AutoMapper defaults (AllowNullCollections=false).
    internal static object? ConvertValue(object? val, Type targetType)
    {
        if (val == null)
            return CreateEmptyCollection(targetType);   // null → empty collection or null

        var srcType = val.GetType();
        if (targetType == srcType) return val;

        // T → Nullable<T>: SetValue boxes it correctly, just return the value
        var underlying = Nullable.GetUnderlyingType(targetType);
        if (underlying != null)
        {
            if (underlying == srcType || underlying.IsAssignableFrom(srcType)) return val;
            try { return Convert.ChangeType(val, underlying); } catch { return null; }
        }

        // Direct upcast / interface assignment
        if (targetType.IsAssignableFrom(srcType))
        {
            // Concrete collection into an interface slot needs a List<T> copy
            // (e.g. string[] → IList<string> must become List<string>)
            if (targetType.IsInterface && GetCollectionElementType(targetType) != null)
                return CopyToList(val, GetCollectionElementType(targetType)!);
            return val;
        }

        // Cross-collection coercions: string[] ↔ List<string>, IList<T> → T[], etc.
        var srcElem = GetCollectionElementType(srcType);
        var dstElem = GetCollectionElementType(targetType);
        if (srcElem != null && dstElem != null && srcElem == dstElem)
            return CopyCollection(val, targetType, dstElem);

        // Numeric / enum / primitive conversion
        try { return Convert.ChangeType(val, targetType); } catch { return val; }
    }

    private static object? CreateEmptyCollection(Type t)
    {
        if (t.IsArray && t.GetArrayRank() == 1)
            return Array.CreateInstance(t.GetElementType()!, 0);
        var elem = GetCollectionElementType(t);
        if (elem != null)
            return Activator.CreateInstance(typeof(List<>).MakeGenericType(elem));
        return null;
    }

    private static object CopyToList(object val, Type elemType)
    {
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elemType))!;
        foreach (var item in (IEnumerable)val) list.Add(item);
        return list;
    }

    private static object CopyCollection(object val, Type targetType, Type elemType)
    {
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elemType))!;
        foreach (var item in (IEnumerable)val) list.Add(item);
        if (targetType.IsArray)
        {
            var arr = Array.CreateInstance(elemType, list.Count);
            list.CopyTo(arr, 0);
            return arr;
        }
        return list;
    }

    private static Type? GetCollectionElementType(Type type)
    {
        if (type == typeof(string)) return null;
        if (type.IsArray && type.GetArrayRank() == 1) return type.GetElementType();
        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(List<>) || def == typeof(IList<>) || def == typeof(ICollection<>)
                || def == typeof(IEnumerable<>) || def == typeof(IReadOnlyList<>)
                || def == typeof(IReadOnlyCollection<>))
                return type.GetGenericArguments()[0];
        }
        foreach (var iface in type.GetInterfaces())
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return iface.GetGenericArguments()[0];
        return null;
    }
}
