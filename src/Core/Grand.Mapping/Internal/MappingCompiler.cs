using System.Linq.Expressions;
using System.Reflection;

namespace Grand.Mapping.Internal;

internal static class MappingCompiler
{
    public static Action<TSource, TDest> Compile<TSource, TDest>(List<MemberConfig> configs)
    {
        var src = Expression.Parameter(typeof(TSource), "src");
        var dst = Expression.Parameter(typeof(TDest), "dst");
        var body = new List<Expression>();

        var direct = new Dictionary<string, MemberConfig>(StringComparer.Ordinal);
        foreach (var c in configs.Where(c => !c.IsPath))
            direct[c.MemberName] = c;

        foreach (var dp in typeof(TDest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite))
        {
            var destAccess = Expression.Property(dst, dp);

            if (direct.TryGetValue(dp.Name, out var mc))
            {
                if (mc.IsIgnored) continue;

                Expression? value = mc.MapFromExpression != null
                    ? Expression.Invoke(mc.MapFromExpression, src)
                    : SourceProp(src, dp.Name);

                if (value == null) continue;
                value = Coerce(value, dp.PropertyType);
                if (value == null) continue;

                var assign = Expression.Assign(destAccess, value);
                body.Add(mc.ConditionExpression != null
                    ? Expression.IfThen(Expression.Invoke(mc.ConditionExpression, src), assign)
                    : (Expression)assign);
            }
            else
            {
                var value = Coerce(SourceProp(src, dp.Name), dp.PropertyType);
                if (value != null)
                    body.Add(Expression.Assign(destAccess, value));
            }
        }

        // ForPath configs
        foreach (var pc in configs.Where(c => c.IsPath && !c.IsIgnored
            && c.MapFromExpression != null && c.DestinationPathExpression != null))
        {
            var destAccess = SubstitutePath(pc.DestinationPathExpression!, dst);
            if (destAccess == null) continue;

            var value = Coerce(Expression.Invoke(pc.MapFromExpression!, src), destAccess.Type);
            if (value == null) continue;

            var assign = Expression.Assign(destAccess, value);
            body.Add(pc.ConditionExpression != null
                ? Expression.IfThen(Expression.Invoke(pc.ConditionExpression, src), assign)
                : (Expression)assign);
        }

        return Expression.Lambda<Action<TSource, TDest>>(
            body.Count > 0 ? (Expression)Expression.Block(body) : Expression.Empty(),
            src, dst).Compile();
    }

    // Returns Expression for same-named readable property on src, or null.
    private static Expression? SourceProp(ParameterExpression src, string name)
    {
        var sp = src.Type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        return sp?.CanRead == true ? Expression.Property(src, sp) : null;
    }

    // Rebuilds (TDest d) => d.Outer.Inner with our dst parameter — no ExpressionVisitor needed.
    private static MemberExpression? SubstitutePath(LambdaExpression pathExpr, ParameterExpression dst)
    {
        var chain = new List<PropertyInfo>();
        var node = pathExpr.Body;
        while (node is MemberExpression me && me.Member is PropertyInfo pi)
        {
            chain.Insert(0, pi);
            node = me.Expression!;
        }
        if (chain.Count == 0 || node is not ParameterExpression) return null;

        Expression result = dst;
        foreach (var pi in chain)
            result = Expression.Property(result, pi);
        return result as MemberExpression;
    }

    // Build-time type coercion inside Expression Tree. Returns null → skip property.
    private static Expression? Coerce(Expression? expr, Type target)
    {
        if (expr == null) return null;
        if (expr.Type == target) return expr;

        // T → Nullable<T>
        var underlyingTarget = Nullable.GetUnderlyingType(target);
        if (underlyingTarget == expr.Type)
            return Expression.Convert(expr, target);

        // Nullable<T> → T
        var underlyingSource = Nullable.GetUnderlyingType(expr.Type);
        if (underlyingSource == target)
            return Expression.Convert(expr, target);

        // Collection coercions with null guard (AutoMapper AllowNullCollections=false behaviour).
        // Runs before IsAssignableFrom to avoid IList<T>/T[] cross-type issues.
        var srcElem = CollectionElementType(expr.Type);
        var dstElem = CollectionElementType(target);
        if (srcElem != null && dstElem != null && srcElem == dstElem)
            return BuildCollectionCoerce(expr, target, dstElem);

        // Direct upcast (no Convert node needed for reference types / value subtypes)
        if (target.IsAssignableFrom(expr.Type)) return expr;

        // Numeric / enum value-type conversion
        if (IsNumericOrEnum(expr.Type) && IsNumericOrEnum(target))
            try { return Expression.Convert(expr, target); } catch { return null; }

        // User-defined conversion operator only (Method != null prevents reference downcast)
        try { var c = Expression.Convert(expr, target); return c.Method != null ? c : null; }
        catch { return null; }
    }

    // null source → empty collection; non-null source → ToArray/ToList copy.
    private static Expression BuildCollectionCoerce(Expression src, Type target, Type elem)
    {
        var iEnum = typeof(IEnumerable<>).MakeGenericType(elem);
        var srcCast = src.Type == iEnum ? src : Expression.Convert(src, iEnum);

        Expression filled, empty;
        if (target.IsArray)
        {
            var toArray = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!.MakeGenericMethod(elem);
            filled = Expression.Call(toArray, srcCast);
            empty = Expression.NewArrayBounds(elem, Expression.Constant(0));
        }
        else
        {
            var listType = typeof(List<>).MakeGenericType(elem);
            var toList = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))!.MakeGenericMethod(elem);
            filled = Expression.Call(toList, srcCast);
            empty = Expression.New(listType);
        }

        if (src.Type.IsValueType) return filled;
        return Expression.Condition(
            Expression.ReferenceEqual(src, Expression.Constant(null, src.Type)),
            empty, filled);
    }

    private static Type? CollectionElementType(Type t)
    {
        if (t == typeof(string)) return null;
        if (t.IsArray && t.GetArrayRank() == 1) return t.GetElementType();
        if (t.IsGenericType)
        {
            var def = t.GetGenericTypeDefinition();
            if (def == typeof(List<>) || def == typeof(IList<>) || def == typeof(ICollection<>)
                || def == typeof(IEnumerable<>) || def == typeof(IReadOnlyList<>)
                || def == typeof(IReadOnlyCollection<>))
                return t.GetGenericArguments()[0];
        }
        foreach (var iface in t.GetInterfaces())
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return iface.GetGenericArguments()[0];
        return null;
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
}
