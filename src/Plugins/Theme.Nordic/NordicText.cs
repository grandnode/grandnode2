using Microsoft.Extensions.Localization;

namespace Theme.Nordic;

/// <summary>
///     Labels the Nordic views render from the plugin's own resources.
///     A theme can be chosen in settings without installing its plugin, and then the resources
///     were never added: the translation service echoes the key back. Such a label falls back
///     to the English value the plugin would have installed.
/// </summary>
public static class NordicText
{
    public static string Get(IStringLocalizer loc, string key)
    {
        var text = loc[key];
        if (text.ResourceNotFound || string.IsNullOrEmpty(text.Value) ||
            string.Equals(text.Value, key, StringComparison.OrdinalIgnoreCase))
            return NordicThemePlugin.Resources.TryGetValue(key, out var fallback) ? fallback : key;

        return text.Value;
    }
}
