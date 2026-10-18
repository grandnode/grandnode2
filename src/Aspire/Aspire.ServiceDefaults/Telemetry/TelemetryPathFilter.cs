using Microsoft.AspNetCore.Http;

namespace Aspire.ServiceDefaults.Telemetry;

public static class TelemetryPathFilter
{
    /// <summary>
    ///     Returns false when the path starts with one of the excluded prefixes or its last segment ends with one of
    ///     the excluded extensions. Prefixes match whole segments only: "/health" excludes "/health/live" but not
    ///     "/healthy".
    /// </summary>
    public static bool ShouldTrace(PathString path, IReadOnlyList<PathString> excludePaths, IReadOnlyList<string> excludeExtensions)
    {
        foreach (var excludePath in excludePaths)
            if (path.StartsWithSegments(excludePath, StringComparison.OrdinalIgnoreCase))
                return false;

        var extension = Path.GetExtension(path.Value.AsSpan());
        if (extension.IsEmpty)
            return true;

        foreach (var excludeExtension in excludeExtensions)
            if (extension.Equals(excludeExtension, StringComparison.OrdinalIgnoreCase))
                return false;

        return true;
    }
}
