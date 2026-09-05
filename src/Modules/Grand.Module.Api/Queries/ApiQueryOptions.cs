using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Grand.Module.Api.Queries;

/// <summary>
///     Raised when a client sends a query option the API refuses to run. Mapped to 400 by the caller;
///     it is never an internal error, so it must not surface as 500.
/// </summary>
public class ApiQueryOptionException(string message) : Exception(message);

/// <summary>
///     Parses and restricts the OData-like query options.
///     $filter reaches a real expression parser, so it is fenced on three sides: a parsing config that
///     denies types, context keywords and object construction; a length limit; and a whitelist that
///     accepts only members of the projected model. $orderby and $select never reach the parser as raw
///     text - they are read as field lists and rebuilt here.
/// </summary>
public static class ApiQueryOptions
{
    /// <summary>
    ///     Long enough for a realistic filter, short enough that a pathological expression cannot make
    ///     the parser the slow part of the request.
    /// </summary>
    public const int MaxFilterLength = 512;

    public const int MaxFields = 50;

    /// <summary>
    ///     Methods a filter may name. Everything here is a cheap, side-effect free string or text
    ///     operation; nothing that reflects, constructs, or walks a collection.
    /// </summary>
    private static readonly HashSet<string> AllowedMethods = new(StringComparer.OrdinalIgnoreCase) {
        "Contains", "StartsWith", "EndsWith", "ToLower", "ToUpper", "Trim", "Length", "Equals"
    };

    /// <summary>
    ///     Operators and literals the parser understands that are not members of the model.
    /// </summary>
    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase) {
        "and", "or", "not", "true", "false", "null", "iif"
    };

    /// <summary>
    ///     A bounded execution budget so a pathological input cannot make regex matching the slow part
    ///     of the request; both patterns below are cheap on well-formed input, so this should never trip.
    /// </summary>
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(200);

    private static readonly Regex Identifier = new(@"[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Compiled, RegexTimeout);

    /// <summary>
    ///     String literals hold user text, not member names, so they are removed before the whitelist
    ///     runs - otherwise a product named "Password" would fail its own search.
    /// </summary>
    private static readonly Regex StringLiteral = new(@"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'", RegexOptions.Compiled, RegexTimeout);

    /// <summary>
    ///     Denies everything the parser can reach outside the model: no type resolution, no `it`/`root`
    ///     context keywords, no `new`, no assembly probing, no Equals/ToString on object.
    /// </summary>
    public static ParsingConfig FilterConfig { get; } = new() {
        AreContextKeywordsEnabled = false,
        AllowNewToEvaluateAnyType = false,
        DisallowNewKeyword = true,
        ResolveTypesBySimpleName = false,
        SupportCastingToFullyQualifiedTypeAsString = false,
        LoadAdditionalAssembliesFromCurrentDomainBaseDirectory = false,
        AllowEqualsAndToStringMethodsOnObject = false,
        RestrictOrderByToPropertyOrField = true,
        CustomTypeProvider = new NoCustomTypesProvider()
    };

    /// <summary>
    ///     $select is rebuilt from validated field names, so `new` has to be available for that one
    ///     projection - and for nothing else.
    /// </summary>
    public static ParsingConfig SelectConfig { get; } = new() {
        AreContextKeywordsEnabled = false,
        AllowNewToEvaluateAnyType = false,
        ResolveTypesBySimpleName = false,
        SupportCastingToFullyQualifiedTypeAsString = false,
        LoadAdditionalAssembliesFromCurrentDomainBaseDirectory = false,
        AllowEqualsAndToStringMethodsOnObject = false,
        RestrictOrderByToPropertyOrField = true,
        CustomTypeProvider = new NoCustomTypesProvider()
    };

    /// <summary>
    ///     Checks that a filter names nothing outside the model it runs against.
    /// </summary>
    public static void ValidateFilter(string filter, Type elementType)
    {
        if (string.IsNullOrWhiteSpace(filter))
            throw new ApiQueryOptionException("$filter is empty");

        if (filter.Length > MaxFilterLength)
            throw new ApiQueryOptionException($"$filter exceeds {MaxFilterLength} characters");

        var members = MemberNames(elementType);
        var expression = StringLiteral.Replace(filter, " ");

        foreach (Match match in Identifier.Matches(expression))
        {
            var name = match.Value;
            if (Keywords.Contains(name) || AllowedMethods.Contains(name) || members.Contains(name))
                continue;

            throw new ApiQueryOptionException($"'{name}' is not a queryable field of {elementType.Name}");
        }
    }

    /// <summary>
    ///     Reads "Field asc, Other desc" and gives it back with every field checked against the model.
    /// </summary>
    public static string ParseOrderBy(string orderBy, Type elementType)
    {
        var members = MemberNames(elementType);
        var parts = SplitFields(orderBy, "$orderby");
        var ordering = new List<string>(parts.Count);

        foreach (var part in parts)
        {
            var tokens = part.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length > 2)
                throw new ApiQueryOptionException($"'{part}' is not a valid $orderby entry");

            if (!members.Contains(tokens[0]))
                throw new ApiQueryOptionException($"'{tokens[0]}' is not a queryable field of {elementType.Name}");

            var direction = tokens.Length == 2 ? tokens[1].ToLowerInvariant() : "asc";
            if (direction != "asc" && direction != "desc")
                throw new ApiQueryOptionException($"'{tokens[1]}' is not a sort direction");

            ordering.Add($"{tokens[0]} {direction}");
        }

        return string.Join(", ", ordering);
    }

    /// <summary>
    ///     Reads a field list and builds the projection itself, so no client text reaches the parser.
    /// </summary>
    public static string ParseSelect(string select, Type elementType)
    {
        var members = MemberNames(elementType);
        var fields = SplitFields(select, "$select");

        foreach (var field in fields)
            if (!members.Contains(field))
                throw new ApiQueryOptionException($"'{field}' is not a queryable field of {elementType.Name}");

        return $"new({string.Join(", ", fields)})";
    }

    private static List<string> SplitFields(string value, string option)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ApiQueryOptionException($"{option} is empty");

        var fields = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .ToList();

        if (fields.Count == 0)
            throw new ApiQueryOptionException($"{option} is empty");

        if (fields.Count > MaxFields)
            throw new ApiQueryOptionException($"{option} lists more than {MaxFields} fields");

        return fields;
    }

    private static HashSet<string> MemberNames(Type elementType)
    {
        return new HashSet<string>(
            elementType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(x => x.Name),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Leaves the parser with no types to resolve at all.
    /// </summary>
    private class NoCustomTypesProvider : IDynamicLinqCustomTypeProvider
    {
        public HashSet<Type> GetCustomTypes()
        {
            return [];
        }

        public Dictionary<Type, List<MethodInfo>> GetExtensionMethods()
        {
            return [];
        }

        public Type ResolveType(string typeName)
        {
            return null;
        }

        public Type ResolveTypeBySimpleName(string simpleTypeName)
        {
            return null;
        }
    }
}
