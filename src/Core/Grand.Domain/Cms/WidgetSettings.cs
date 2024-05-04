using Grand.Domain.Configuration;

namespace Grand.Domain.Cms;

public class WidgetSettings : ISettings
{
    /// <summary>
    ///     Gets or sets a system names of active widgets
    /// </summary>
    public List<string> ActiveWidgetSystemNames { get; set; } = new();
}