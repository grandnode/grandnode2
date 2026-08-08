namespace Grand.Infrastructure.Plugins;

[AttributeUsage(AttributeTargets.Assembly)]
public class PluginInfoAttribute : Attribute
{
    public string Group { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;

    /// <summary>
    ///     The GrandNode version this plugin supports, as "Major.Minor". Left unset by convention -
    ///     it is then resolved from the assembly's Grand.Infrastructure reference by
    ///     <see cref="PluginVersionResolver" />.
    /// </summary>
    public string SupportedVersion { get; set; }

    public string Version { get; set; }
}