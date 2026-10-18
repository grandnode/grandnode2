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

    /// <summary>
    ///     Content types served from the Plugins folder: static assets and documents (pdf) only. The folder also holds the plugin
    ///     assemblies with their deps.json and pdb files, which must never be served.
    /// </summary>
    public static IContentTypeProvider Provider { get; } = CreateProvider();

    private static FileExtensionContentTypeProvider CreateProvider()
    {
        var defaults = new FileExtensionContentTypeProvider().Mappings;
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var extension in AssetExtensions)
            mappings[extension] = defaults.TryGetValue(extension, out var contentType)
                ? contentType
                : "application/octet-stream";

        return new FileExtensionContentTypeProvider(mappings);
    }
}
