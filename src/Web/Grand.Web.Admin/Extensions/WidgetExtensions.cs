using Grand.Business.Cms.Interfaces;
using Grand.Domain.Cms;
using System;

namespace Grand.Web.Admin.Extensions
{
    public static class WidgetExtensions
    {
        public static bool IsWidgetActive(this IWidgetProvider widget,
            WidgetSettings widgetSettings)
        {
            if (widget == null)
                throw new ArgumentNullException(nameof(widget));

            if (widgetSettings == null)
                throw new ArgumentNullException(nameof(widgetSettings));

            if (widgetSettings.ActiveWidgetSystemNames == null)
                return false;
            foreach (string activeMethodSystemName in widgetSettings.ActiveWidgetSystemNames)
                if (widget.SystemName.Equals(activeMethodSystemName, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}
