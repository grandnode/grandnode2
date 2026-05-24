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

        var direct = BuildDirectConfigLookup(configs);

        foreach (var dp in typeof(TDest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite))
        {
            var destAccess = Expression.Property(dst, dp);
            if (direct.TryGetValue(dp.Name, out var mc))
                ProcessMappedProperty(body, dp, mc, src, destAccess, registeredTypes, mappings);
            else
                ProcessAutoProperty(body, dp, src, destAccess, registeredTypes, mappings);
        }

        ProcessForPaths(body, configs, src, dst);

        return Expression.Lambda<Action<TSource, TDest>>(
            body.Count > 0 ? (Expression)Expression.Block(body) : Expression.Empty(),
            src, dst).Compile();
    }

    private static Dictionary<string, MemberConfig> BuildDirectConfigLookup(List<MemberConfig> configs)
    {
        var direct = new Dictionary<string, MemberConfig>(StringComparer.Ordinal);
        foreach (var c in configs.Where(c => !c.IsPath))
            direct[c.MemberName] = c;
        return direct;
    }

    private static void ProcessMappedProperty(
        List<Expression> body, PropertyInfo dp, MemberConfig mc,
        ParameterExpression src, Expression destAccess,
        HashSet<(Type, Type)> registeredTypes,
        Dictionary<(Type, Type), Delegate> mappings)
    {
        if (mc.IsIgnored) return;

        var value = mc.MapFromExpression != null
            ? InlineLambda(mc.MapFromExpression, src)
            : SourceProp(src, dp.Name);

        if (value == null) return;

        var expr = BuildAssignOrNested(value, destAccess, dp.PropertyType,
            mc.ConditionExpression, src, registeredTypes, mappings);
        if (expr != null) body.Add(expr);
    }

    private static void ProcessAutoProperty(
        List<Expression> body, PropertyInfo dp,
        ParameterExpression src, Expression destAccess,
        HashSet<(Type, Type)> registeredTypes,
        Dictionary<(Type, Type), Delegate> mappings)
    {
        var srcExpr = SourceProp(src, dp.Name);
        if (srcExpr == null) return;

        var expr = BuildAssignOrNested(srcExpr, destAccess, dp.PropertyType,
            null, src, registeredTypes, mappings);
        if (expr != null) body.Add(expr);
    }

    private static Expression? BuildAssignOrNested(
        Expression value, Expression destAccess, Type destType,
        LambdaExpression? condition, ParameterExpression src,
        HashSet<(Type, Type)> registeredTypes,
        Dictionary<(Type, Type), Delegate> mappings)
    {
        var coerced = Coerce(value, destType);
        if (coerced != null)
            return WrapWithCondition(Expression.Assign(destAccess, coerced), condition, src);

        return BuildNestedMapping(value, destAccess, destType, condition, src, registeredTypes, mappings);
    }

    private static void ProcessForPaths(
        List<Expression> body, List<MemberConfig> configs,
        ParameterExpression src, ParameterExpression dst)
    {
        foreach (var pc in configs.Where(c => c.IsPath && !c.IsIgnored
            && c.MapFromExpression != null && c.DestinationPathExpression != null))
        {
            var expr = BuildForPathExpression(pc, src, dst);
            if (expr != null) body.Add(expr);
        }
    }

    private static Expression? BuildForPathExpression(
        MemberConfig pc, ParameterExpression src, ParameterExpression dst)
    {
        var destAccess = SubstitutePath(pc.DestinationPathExpression!, dst);
        if (destAccess == null) return null;

        var value = Coerce(InlineLambda(pc.MapFromExpression!, src), destAccess.Type);
        if (value == null) return null;

        return WrapWithCondition(Expression.Assign(destAccess, value), pc.ConditionExpression, src);
    }

    private static Expression WrapWithCondition(
        Expression expr, LambdaExpression? condition, ParameterExpression src)
        => condition != null
            ? Expression.IfThen(InlineLambda(condition, src), expr)
            : expr;

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

        if (IsDirectObjectMapping(srcType, destType, registeredTypes))
        {
            var innerBlock = BuildDirectObjectMappingBlock(
                srcValueExpr, destAccess, srcType, destType, mappings);
            return WrapWithCondition(innerBlock, condition, srcParam);
        }

        var srcElem = CollectionElementType(srcType);
        var dstElem = CollectionElementType(destType);
        if (srcElem != null && dstElem != null && srcElem != dstElem
            && registeredTypes.Contains((srcElem, dstElem)))
        {
            var converted = BuildCrossTypeCollectionCoerce(srcValueExpr, destType, srcElem, dstElem, mappings);
            return WrapWithCondition(Expression.Assign(destAccess, converted), condition, srcParam);
        }

        return null;
    }

    private static bool IsDirectObjectMapping(
        Type srcType, Type destType, HashSet<(Type, Type)> registeredTypes)
        => !srcType.IsValueType && !destType.IsValueType
           && CollectionElementType(srcType) == null
           && CollectionElementType(destType) == null
           && destType.GetConstructor(Type.EmptyTypes) != null
           && registeredTypes.Contains((srcType, destType));

    private static Expression BuildDirectObjectMappingBlock(
        Expression srcValueExpr, Expression destAccess,
        Type srcType, Type destType,
        Dictionary<(Type, Type), Delegate> mappings)
    {
        var castDel = Expression.Convert(
            Expression.Call(Expression.Constant(mappings), _dictGetItemMethod,
                Expression.Constant((srcType, destType))),
            typeof(Action<,>).MakeGenericType(srcType, destType));

        var srcVar = Expression.Variable(srcType, "ns");
        var tmpVar = Expression.Variable(destType, "nd");

        return Expression.Block(
            new[] { srcVar, tmpVar },
            Expression.Assign(srcVar, srcValueExpr),
            Expression.IfThen(
                Expression.ReferenceNotEqual(srcVar, Expression.Constant(null, srcType)),
                Expression.Block(
                    Expression.Assign(tmpVar, Expression.New(destType)),
                    Expression.Invoke(castDel, srcVar, tmpVar),
                    Expression.Assign(destAccess, tmpVar))));
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

    private static Expression InlineLambda(LambdaExpression lambda, Expression arg)
        => new ParameterReplacer(lambda.Parameters[0], arg).Visit(lambda.Body);

    private static Expression? SourceProp(ParameterExpression src, string name)
    {
        var sp = src.Type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        return sp?.CanRead == true ? Expression.Property(src, sp) : null;
    }

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

    private static Expression? Coerce(Expression? expr, Type target)
    {
        if (expr == null) return null;
        if (expr.Type == target) return expr;

        // T → Nullable<T>
        var underlyingTarget = Nullable.GetUnderlyingType(target);
        if (underlyingTarget == expr.Type)
            return Expression.Convert(expr, target);

        return CoerceNullableToValue(expr, target)
            ?? CoerceCollections(expr, target)
            ?? CoerceUpcast(expr, target)
            ?? CoerceNumericOrEnum(expr, target)
            ?? CoerceViaConvertOperator(expr, target);
    }

    private static Expression? CoerceNullableToValue(Expression expr, Type target)
    {
        var underlyingSource = Nullable.GetUnderlyingType(expr.Type);
        if (underlyingSource != target) return null;
        return Expression.Condition(
            Expression.Property(expr, nameof(Nullable<int>.HasValue)),
            Expression.Property(expr, nameof(Nullable<int>.Value)),
            Expression.Default(target));
    }

    private static Expression? CoerceCollections(Expression expr, Type target)
    {
        var srcElem = CollectionElementType(expr.Type);
        var dstElem = CollectionElementType(target);
        if (srcElem == null || dstElem == null || srcElem != dstElem) return null;
        return BuildCollectionCoerce(expr, target, dstElem);
    }

    private static Expression? CoerceUpcast(Expression expr, Type target)
        => target.IsAssignableFrom(expr.Type) ? expr : null;

    private static Expression? CoerceNumericOrEnum(Expression expr, Type target)
    {
        if (!IsNumericOrEnum(expr.Type) || !IsNumericOrEnum(target)) return null;
        try { return Expression.Convert(expr, target); } catch { return null; }
    }

    private static Expression? CoerceViaConvertOperator(Expression expr, Type target)
    {
        try { var c = Expression.Convert(expr, target); return c.Method != null ? c : null; }
        catch { return null; }
    }

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
