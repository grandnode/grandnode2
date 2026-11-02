namespace Grand.Infrastructure.Plugins;

/// <summary>
///     The <see cref="PluginInfoAttribute" /> values of a plugin assembly, read without loading it.
/// </summary>
/// <param name="SupportedVersion">
///     Already resolved by <see cref="PluginVersionResolver" />; null when it cannot be determined.
/// </param>
public sealed record PluginManifest(
    string SystemName,
    string FriendlyName,
    string Group,
    string Author,
    string Version,
    string SupportedVersion);
