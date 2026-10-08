using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common.Security.Oidc;

public static class OidcPath
{
    /// <summary>
    ///     "/api/" -> "/api"; null when empty, relative or the site root (a root prefix would capture the storefront)
    /// </summary>
    public static string Normalize(string prefix)
    {
        var trimmed = prefix?.Trim();
        if (string.IsNullOrEmpty(trimmed) || !trimmed.StartsWith('/'))
            return null;
        trimmed = trimmed.TrimEnd('/');
        return trimmed.Length == 0 ? null : trimmed;
    }

    /// <summary>
    ///     Whether the normalized prefix covers the path on a segment boundary, ignoring case
    /// </summary>
    public static bool Covers(string normalizedPrefix, PathString path) =>
        normalizedPrefix != null && path.HasValue &&
        path.StartsWithSegments(new PathString(normalizedPrefix), StringComparison.OrdinalIgnoreCase);
}
