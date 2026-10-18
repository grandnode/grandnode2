using Microsoft.AspNetCore.StaticFiles;

namespace Grand.Web.Common.Infrastructure;

public static class PluginAssetContentTypes
{
    private static readonly string[] AssetExtensions =
    [
        ".css", ".js", ".map",
        ".jpg", ".jpeg", ".png", ".gif", ".svg", ".webp", ".avif", ".ico",
        ".woff", ".woff2", ".ttf", ".otf", ".eot",
        ".pdf"
    ];

    //plugin assemblies and their build files share the folder - never served, whatever the configuration says
    private static readonly string[] NeverServedExtensions = [".dll", ".pdb", ".exe", ".config"];

    /// <summary>
    ///     Content types served from the Plugins folder: static assets and documents (pdf), plus the extensions
    ///     configured in Application:PluginStaticFileExtensions. The folder also holds the plugin assemblies with
    ///     their deps.json and pdb files, which must never be served.
    /// </summary>
    /// <param name="additionalExtensions">Extensions to serve on top of the built-in ones, with or without the dot</param>
    public static IContentTypeProvider Create(IEnumerable<string> additionalExtensions)
    {
        var defaults = new FileExtensionContentTypeProvider().Mappings;
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var extensions = AssetExtensions.Concat((additionalExtensions ?? [])
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Select(e => e.StartsWith('.') ? e : "." + e));

        foreach (var extension in extensions)
        {
            if (NeverServedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                continue;

            mappings[extension] = defaults.TryGetValue(extension, out var contentType)
                ? contentType
                : "application/octet-stream";
        }

        return new FileExtensionContentTypeProvider(mappings);
    }
}
