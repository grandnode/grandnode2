using System.Text.RegularExpressions;

namespace Grand.Module.Scim.Protocol;

/// <summary>
///     The one SCIM filter form the identity providers use to find a resource: <c>attribute eq "value"</c>. Attribute and
///     operator in any letter case; anything else (other operators, and/or, presence) is refused
/// </summary>
/// <param name="Attribute">Lower-cased attribute name</param>
public partial record ScimFilter(string Attribute, string Value)
{
    [GeneratedRegex("""^\s*([A-Za-z][A-Za-z0-9._:]*)\s+eq\s+"((?:[^"\\]|\\.)*)"\s*$""", RegexOptions.IgnoreCase)]
    private static partial Regex EqualityFilter();

    [GeneratedRegex("""\\(.)""")]
    private static partial Regex Escape();

    public static bool TryParse(string filter, out ScimFilter result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(filter))
            return false;

        var match = EqualityFilter().Match(filter);
        if (!match.Success)
            return false;

        result = new ScimFilter(match.Groups[1].Value.ToLowerInvariant(),
            Escape().Replace(match.Groups[2].Value, "$1"));
        return true;
    }
}
