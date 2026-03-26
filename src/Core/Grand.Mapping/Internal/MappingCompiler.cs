using System.Linq.Expressions;
using System.Reflection;

namespace Grand.Mapping.Internal;

internal static class MappingCompiler
{
    public static Action<TSource, TDest> Compile<TSource, TDest>(List<MemberConfig> configs)
    {
        var srcParam = Expression.Parameter(typeof(TSource), "src");
        var dstParam = Expression.Parameter(typeof(TDest), "dst");
        var statements = new List<Expression>();

        // Index non-path configs by member name (last ForMember wins for duplicates)
        var directConfigs = new Dictionary<string, MemberConfig>(StringComparer.Ordinal);
        foreach (var c in configs.Where(c => !c.IsPath))
            directConfigs[c.MemberName] = c;

        // Process all writable destination properties
        var destProps = typeof(TDest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);

        foreach (var destProp in destProps)
        {
            if (directConfigs.TryGetValue(destProp.Name, out var config))
            {
                if (config.IsIgnored) continue;

                Expression? valueExpr;

                if (config.MapFromExpression != null)
                {
                    // Custom mapping: inline the MapFrom lambda body with substituted src parameter
                    valueExpr = ParameterReplacer.Replace(
                        config.MapFromExpression.Body,
                        config.MapFromExpression.Parameters[0],
                        srcParam);
                }
                else
                {
                    // Condition-only: auto-map from same-named source property
                    var srcProp = typeof(TSource).GetProperty(destProp.Name,
                        BindingFlags.Public | BindingFlags.Instance);
                    if (srcProp == null || !srcProp.CanRead) continue;
                    valueExpr = Expression.Property(srcParam, srcProp);
                }

                valueExpr = TryConvert(valueExpr, destProp.PropertyType);
                if (valueExpr == null) continue;

                var dstAccess = Expression.Property(dstParam, destProp);
                var assignment = Expression.Assign(dstAccess, valueExpr);
                statements.Add(WrapInCondition(assignment, config.ConditionExpression, srcParam));
            }
            else
            {
                // Auto-mapping: same-named property in source with compatible type
                var srcProp = typeof(TSource).GetProperty(destProp.Name,
                    BindingFlags.Public | BindingFlags.Instance);
                if (srcProp == null || !srcProp.CanRead) continue;

                var valueExpr = TryConvert(
                    Expression.Property(srcParam, srcProp),
                    destProp.PropertyType);
                if (valueExpr == null) continue;

                var dstAccess = Expression.Property(dstParam, destProp);
                statements.Add(Expression.Assign(dstAccess, valueExpr));
            }
        }

        // Process ForPath configs
        foreach (var config in configs.Where(c => c.IsPath && !c.IsIgnored))
        {
            if (config.DestinationPathExpression == null || config.MapFromExpression == null) continue;

            var destAccess = ParameterReplacer.Replace(
                config.DestinationPathExpression.Body,
                config.DestinationPathExpression.Parameters[0],
                dstParam);

            if (destAccess is not MemberExpression memberAccess) continue;

            var valueExpr = ParameterReplacer.Replace(
                config.MapFromExpression.Body,
                config.MapFromExpression.Parameters[0],
                srcParam);

            valueExpr = TryConvert(valueExpr, memberAccess.Type) ?? valueExpr;

            var assignment = Expression.Assign(destAccess, valueExpr);
            statements.Add(WrapInCondition(assignment, config.ConditionExpression, srcParam));
        }

        var body = statements.Count > 0
            ? (Expression)Expression.Block(statements)
            : Expression.Empty();

        return Expression.Lambda<Action<TSource, TDest>>(body, srcParam, dstParam).Compile();
    }

    private static Expression? TryConvert(Expression expr, Type targetType)
    {
        if (expr.Type == targetType) return expr;

        // T → Nullable<T>: requires explicit Convert node for Expression.Assign.
        if (targetType.IsGenericType
            && targetType.GetGenericTypeDefinition() == typeof(Nullable<>)
            && targetType.GetGenericArguments()[0] == expr.Type)
        {
            return Expression.Convert(expr, targetType);
        }

        // Nullable<T> → T: same reason.
        var underlyingTarget = Nullable.GetUnderlyingType(targetType);
        if (underlyingTarget != null && underlyingTarget == expr.Type)
            return Expression.Convert(expr, targetType);

        // Collection coercions: IEnumerable<T>/IList<T>/etc. → T[] or List<T>.
        // AutoMapper handles these implicitly; we do the same via Enumerable.ToArray/ToList.
        var collectionExpr = TryBuildCollectionConversion(expr, targetType);
        if (collectionExpr != null) return collectionExpr;

        // Direct upcast — no Convert node needed.
        if (targetType.IsAssignableFrom(expr.Type)) return expr;

        // Numeric / enum value-type conversions.
        if (IsNumericOrEnum(expr.Type) && IsNumericOrEnum(targetType))
        {
            try { return Expression.Convert(expr, targetType); }
            catch { return null; }
        }

        // User-defined conversion operator (Method != null).
        // Do NOT fall back to a raw reference cast (Method == null) for incompatible
        // reference types such as List<T> → T[] — it compiles but throws InvalidCastException.
        try
        {
            var conv = Expression.Convert(expr, targetType);
            return conv.Method != null ? conv : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Builds a null-safe collection conversion expression:
    ///   IEnumerable&lt;T&gt; → T[]    via  src == null ? null : Enumerable.ToArray(src)
    ///   IEnumerable&lt;T&gt; → List&lt;T&gt; via  src == null ? null : Enumerable.ToList(src)
    /// Returns null when the types are not a supported collection pair.
    /// </summary>
    private static Expression? TryBuildCollectionConversion(Expression expr, Type targetType)
    {
        // Determine source element type from IEnumerable<T>
        var srcElementType = GetEnumerableElementType(expr.Type);
        if (srcElementType == null) return null;

        // Target: T[]
        if (targetType.IsArray && targetType.GetArrayRank() == 1)
        {
            var destElementType = targetType.GetElementType()!;
            if (srcElementType != destElementType) return null;

            var toArray = typeof(Enumerable)
                .GetMethod(nameof(Enumerable.ToArray))!
                .MakeGenericMethod(destElementType);

            return BuildNullSafeCall(expr, targetType, toArray);
        }

        // Target: List<T>
        if (targetType.IsGenericType
            && targetType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var destElementType = targetType.GetGenericArguments()[0];
            if (srcElementType != destElementType) return null;

            var toList = typeof(Enumerable)
                .GetMethod(nameof(Enumerable.ToList))!
                .MakeGenericMethod(destElementType);

            return BuildNullSafeCall(expr, targetType, toList);
        }

        return null;
    }

    private static Type? GetEnumerableElementType(Type type)
    {
        if (type == typeof(string)) return null; // string is IEnumerable<char>, skip
        if (type.IsArray) return type.GetElementType();

        // Direct generic: IEnumerable<T>, IList<T>, ICollection<T>, etc.
        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(IEnumerable<>) || def == typeof(IList<>)
                || def == typeof(ICollection<>) || def == typeof(IReadOnlyList<>)
                || def == typeof(IReadOnlyCollection<>) || def == typeof(List<>))
            {
                return type.GetGenericArguments()[0];
            }
        }

        // Interface implemented: find IEnumerable<T>
        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType
                && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return iface.GetGenericArguments()[0];
            }
        }

        return null;
    }

    // Generates: src == null ? (T[])null : Enumerable.ToArray(src)
    private static Expression BuildNullSafeCall(Expression expr, Type targetType, MethodInfo method)
    {
        if (expr.Type.IsValueType)
            return Expression.Call(method, expr);

        var iEnumType = method.GetParameters()[0].ParameterType;
        var srcArg = expr.Type == iEnumType ? expr : Expression.Convert(expr, iEnumType);

        return Expression.Condition(
            Expression.ReferenceEqual(expr, Expression.Constant(null, expr.Type)),
            Expression.Constant(null, targetType),
            Expression.Call(method, srcArg));
    }

    private static bool IsNumericOrEnum(Type t)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;
        return t.IsEnum || t == typeof(byte) || t == typeof(sbyte)
            || t == typeof(short) || t == typeof(ushort)
            || t == typeof(int) || t == typeof(uint)
            || t == typeof(long) || t == typeof(ulong)
            || t == typeof(float) || t == typeof(double)
            || t == typeof(decimal);
    }

    private static Expression WrapInCondition(
        Expression body,
        LambdaExpression? condition,
        ParameterExpression srcParam)
    {
        if (condition == null) return body;
        var condBody = ParameterReplacer.Replace(condition.Body, condition.Parameters[0], srcParam);
        return Expression.IfThen(condBody, body);
    }
}
