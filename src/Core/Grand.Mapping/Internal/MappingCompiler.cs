#nullable enable

using System.Linq.Expressions;
using System.Reflection;

namespace Grand.Mapping.Internal;

internal static class MappingCompiler
{
    private static readonly MethodInfo _dictGetItemMethod =
        typeof(Dictionary<(Type, Type), Delegate>).GetMethod("get_Item")!;

    public static Action<TSource, TDest> Compile<TSource, TDest>(
        List<MemberConfig> configs,
        HashSet<(Type, Type)> registeredTypes,
        Dictionary<(Type, Type), Delegate> mappings)
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
                    ? InlineLambda(mc.MapFromExpression, src)
                    : SourceProp(src, dp.Name);

                if (value == null) continue;

                var coerced = Coerce(value, dp.PropertyType);
                if (coerced != null)
                {
                    var assign = Expression.Assign(destAccess, coerced);
                    body.Add(mc.ConditionExpression != null
                        ? Expression.IfThen(InlineLambda(mc.ConditionExpression, src), assign)
                        : (Expression)assign);
                }
                else
                {
                    // Fallback: delegate cross-type mapping to a registered profile
                    var nested = BuildNestedMapping(value, destAccess, dp.PropertyType,
                        mc.ConditionExpression, src, registeredTypes, mappings);
                    if (nested != null) body.Add(nested);
                }
            }
            else
            {
                var srcExpr = SourceProp(src, dp.Name);
                if (srcExpr == null) continue;

                var value = Coerce(srcExpr, dp.PropertyType);
                if (value != null)
                    body.Add(Expression.Assign(destAccess, value));
                else
                {
                    // Fallback: delegate cross-type mapping to a registered profile
                    var nested = BuildNestedMapping(srcExpr, destAccess, dp.PropertyType,
                        null, src, registeredTypes, mappings);
                    if (nested != null) body.Add(nested);
                }
            }
        }

        // ForPath configs
        foreach (var pc in configs.Where(c => c.IsPath && !c.IsIgnored
            && c.MapFromExpression != null && c.DestinationPathExpression != null))
        {
            var destAccess = SubstitutePath(pc.DestinationPathExpression!, dst);
            if (destAccess == null) continue;

            var value = Coerce(InlineLambda(pc.MapFromExpression!, src), destAccess.Type);
            if (value == null) continue;

            var assign = Expression.Assign(destAccess, value);
            body.Add(pc.ConditionExpression != null
                ? Expression.IfThen(InlineLambda(pc.ConditionExpression, src), assign)
                : (Expression)assign);
        }

        return Expression.Lambda<Action<TSource, TDest>>(
            body.Count > 0 ? (Expression)Expression.Block(body) : Expression.Empty(),
            src, dst).Compile();
    }

    /// <summary>
    /// Generates an expression that maps srcValueExpr → destAccess using a registered
    /// profile mapping. Handles both single objects (A → B) and collections (IList&lt;A&gt; → IList&lt;B&gt;).
    /// Returns null when no applicable registered mapping exists.
    /// </summary>
    private static Expression? BuildNestedMapping(
        Expression srcValueExpr,
        Expression destAccess,
        Type destType,
        LambdaExpression? condition,
        ParameterExpression srcParam,
        HashSet<(Type, Type)> registeredTypes,
        Dictionary<(Type, Type), Delegate> mappings)
    {
        var srcType = srcValueExpr.Type;

        // Case 1: A → B (both non-value, non-collection reference types with registered mapping)
        if (!srcType.IsValueType && !destType.IsValueType
            && CollectionElementType(srcType) == null
            && CollectionElementType(destType) == null
            && destType.GetConstructor(Type.EmptyTypes) != null
            && registeredTypes.Contains((srcType, destType)))
        {
            // Capture the dictionary so the delegate can be looked up at runtime
            // (when all delegates are guaranteed to be compiled).
            var delConst = Expression.Constant(mappings);
            var keyConst = Expression.Constant((srcType, destType));
            var getDel = Expression.Call(
                delConst,
                _dictGetItemMethod,
                keyConst);
            var castDel = Expression.Convert(
                getDel,
                typeof(Action<,>).MakeGenericType(srcType, destType));

            // Cache source value to avoid double-evaluation (e.g. when srcValueExpr is Invoke)
            var srcVar = Expression.Variable(srcType, "ns");
            var tmpVar = Expression.Variable(destType, "nd");

            var innerBlock = Expression.Block(
                new[] { srcVar, tmpVar },
                Expression.Assign(srcVar, srcValueExpr),
                Expression.IfThen(
                    Expression.ReferenceNotEqual(srcVar, Expression.Constant(null, srcType)),
                    Expression.Block(
                        Expression.Assign(tmpVar, Expression.New(destType)),
                        Expression.Invoke(castDel, srcVar, tmpVar),
                        Expression.Assign(destAccess, tmpVar))));

            return condition != null
                ? Expression.IfThen(InlineLambda(condition, srcParam), innerBlock)
                : (Expression)innerBlock;
        }

        // Case 2: IList<A> → IList<B> / A[] → B[] where A→B mapping is registered
        var srcElem = CollectionElementType(srcType);
        var dstElem = CollectionElementType(destType);
        if (srcElem != null && dstElem != null && srcElem != dstElem
            && registeredTypes.Contains((srcElem, dstElem)))
        {
            var converted = BuildCrossTypeCollectionCoerce(srcValueExpr, destType, srcElem, dstElem, mappings);
            Expression assignExpr = Expression.Assign(destAccess, converted);
            return condition != null
                ? Expression.IfThen(InlineLambda(condition, srcParam), assignExpr)
                : assignExpr;
        }

        return null;
    }

    private static Expression BuildCrossTypeCollectionCoerce(
        Expression src,
        Type destType,
        Type srcElem,
        Type dstElem,
        Dictionary<(Type, Type), Delegate> mappings)
    {
        var delConst = Expression.Constant(mappings);
        var keyConst = Expression.Constant((srcElem, dstElem));
        var getDel = Expression.Call(
            delConst,
            _dictGetItemMethod,
            keyConst);
        var castDel = Expression.Convert(getDel, typeof(Action<,>).MakeGenericType(srcElem, dstElem));

        // x => { var tmp = new DstElem(); del(x, tmp); return tmp; }
        var xParam = Expression.Parameter(srcElem, "x");
        var tmpVar = Expression.Variable(dstElem, "tmp");
        var selectorBody = Expression.Block(
            new[] { tmpVar },
            Expression.Assign(tmpVar, Expression.New(dstElem)),
            Expression.Invoke(castDel, xParam, tmpVar),
            tmpVar);
        var selector = Expression.Lambda(selectorBody, xParam);

        var iEnumSrc = typeof(IEnumerable<>).MakeGenericType(srcElem);
        var srcCast = src.Type == iEnumSrc ? src : Expression.Convert(src, iEnumSrc);

        var selectMethod = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == nameof(Enumerable.Select) && m.GetParameters().Length == 2)
            .MakeGenericMethod(srcElem, dstElem);

        Expression filled;
        if (destType.IsArray)
        {
            var toArray = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!.MakeGenericMethod(dstElem);
            filled = Expression.Call(toArray, Expression.Call(selectMethod, srcCast, selector));
        }
        else
        {
            var toList = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))!.MakeGenericMethod(dstElem);
            filled = Expression.Call(toList, Expression.Call(selectMethod, srcCast, selector));
        }

        Expression emptyExpr = destType.IsArray
            ? Expression.NewArrayBounds(dstElem, Expression.Constant(0))
            : (Expression)Expression.New(typeof(List<>).MakeGenericType(dstElem));

        if (src.Type.IsValueType) return filled;
        return Expression.Condition(
            Expression.ReferenceEqual(src, Expression.Constant(null, src.Type)),
            emptyExpr,
            filled);
    }

    // Inlines a single-parameter lambda body by substituting the parameter,
    // avoiding an Expression.Invoke delegate-call wrapper in the compiled tree.
    private static Expression InlineLambda(LambdaExpression lambda, Expression arg)
        => new ParameterReplacer(lambda.Parameters[0], arg).Visit(lambda.Body);

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
