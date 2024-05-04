using Grand.Business.Core.Interfaces.Cms;
using Grand.Domain.Cms;

namespace Grand.Web.Admin.Extensions;

public static class WidgetExtensions
{
    public static bool IsWidgetActive(this IWidgetProvider widget,
        WidgetSettings widgetSettings)
    {
        ArgumentNullException.ThrowIfNull(widget);
        ArgumentNullException.ThrowIfNull(widgetSettings);

        if (widgetSettings.ActiveWidgetSystemNames == null)
            return false;
        foreach (var activeMethodSystemName in widgetSettings.ActiveWidgetSystemNames)
            if (widget.SystemName.Equals(activeMethodSystemName, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}