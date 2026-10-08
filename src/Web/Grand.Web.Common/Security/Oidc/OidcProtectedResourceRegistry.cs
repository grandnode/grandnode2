using Grand.Business.Core.Interfaces.Authentication;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common.Security.Oidc;

/// <summary>
///     Protected resources from appsettings and from installed plugins, validated once: unique names, unique and
///     non-overlapping prefixes. Invalid entries are left out and reported in <see cref="Errors" />.
/// </summary>
public class OidcProtectedResourceRegistry : IOidcProtectedResourceRegistry
{
    private readonly List<(string Prefix, OidcProtectedResource Resource)> _entries = [];
    private readonly List<string> _errors = [];

    public OidcProtectedResourceRegistry(OidcResourceServerConfig config,
        IEnumerable<IOidcProtectedResourceProvider> providers)
    {
        if (config is not { Enabled: true })
            return;

        foreach (var resource in config.Resources ?? [])
            Add(resource, "configuration");

        foreach (var provider in providers)
        {
            List<OidcProtectedResource> resources;
            try
            {
                resources = provider.GetResources()?.ToList() ?? [];
            }
            catch (Exception ex)
            {
                _errors.Add($"{provider.GetType().FullName} failed to return its resources: {ex.Message}");
                continue;
            }

            foreach (var resource in resources)
                Add(resource, provider.GetType().FullName);
        }
    }

    public IReadOnlyList<OidcProtectedResource> Resources => _entries.Select(x => x.Resource).ToList();
    public IReadOnlyList<string> Errors => _errors;

    public OidcProtectedResource Match(PathString path)
    {
        foreach (var (prefix, resource) in _entries)
            if (OidcPath.Covers(prefix, path))
                return resource;
        return null;
    }

    private void Add(OidcProtectedResource resource, string source)
    {
        if (resource == null)
            return;

        var prefix = OidcPath.Normalize(resource.PathPrefix);
        if (string.IsNullOrWhiteSpace(resource.Name) || prefix == null)
        {
            _errors.Add($"Protected resource '{resource.Name}' from {source}: a name and a path prefix starting with '/' (not '/' alone) are required.");
            return;
        }

        if (_entries.Any(x => string.Equals(x.Resource.Name, resource.Name, StringComparison.OrdinalIgnoreCase)))
        {
            _errors.Add($"Protected resource '{resource.Name}' from {source}: the name is already used.");
            return;
        }

        var clash = _entries.FirstOrDefault(x =>
            OidcPath.Covers(x.Prefix, new PathString(prefix)) || OidcPath.Covers(prefix, new PathString(x.Prefix)));
        if (clash.Resource != null)
        {
            _errors.Add($"Protected resource '{resource.Name}' from {source}: path '{prefix}' overlaps '{clash.Prefix}' of '{clash.Resource.Name}'.");
            return;
        }

        _entries.Add((prefix, resource));
    }
}
