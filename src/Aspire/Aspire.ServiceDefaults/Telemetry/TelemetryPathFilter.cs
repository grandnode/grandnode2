using Microsoft.AspNetCore.Http;

namespace Aspire.ServiceDefaults.Telemetry;

public static class TelemetryPathFilter
{
    /// <summary>
    ///     Returns false when the path starts with one of the excluded prefixes. Whole segments only:
    ///     "/health" excludes "/health/live" but not "/healthy".
    /// </summary>
    public static bool ShouldTrace(PathString path, IReadOnlyList<string> excludePaths)
    {
        foreach (var excludePath in excludePaths)
            if (path.StartsWithSegments(excludePath, StringComparison.OrdinalIgnoreCase))
                return false;

        return true;
    }
}
