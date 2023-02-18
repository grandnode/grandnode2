using Microsoft.AspNetCore.Mvc.Razor;

namespace Grand.Web.Common.Themes
{
    public class ThemeViewLocationExpander : IViewLocationExpander
    {
        private const string ThemeKey = "Theme";
        private const string AreaAdminKey = "AdminTheme";


        public void PopulateValues(ViewLocationExpanderContext context)
        {
            var themeContext = (IThemeContext)context.ActionContext.HttpContext.RequestServices.GetService(typeof(IThemeContext));

            if (context.AreaName?.Equals("Admin") ?? false)
                context.Values[AreaAdminKey] = themeContext?.AdminAreaThemeName;
            else
                context.Values[ThemeKey] = themeContext?.WorkingThemeName;
        }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            if (context.AreaName == null && context.Values.TryGetValue(ThemeKey, out var theme))
            {
                viewLocations = new[] {
                        $"/Themes/{theme}/Views/{{1}}/{{0}}.cshtml",
                        $"/Themes/{theme}/Views/Shared/{{0}}.cshtml"
                    }
                    .Concat(viewLocations);
            }
            if ((context.AreaName?.Equals("Admin") ?? false) && context.Values.TryGetValue(AreaAdminKey, out var adminTheme))
            {
                viewLocations = new[] {
                        $"/Areas/{{2}}/Themes/{adminTheme}/Views/{{1}}/{{0}}.cshtml",
                        $"/Areas/{{2}}/Themes/{adminTheme}/Views/Shared/{{0}}.cshtml"
                    }
                    .Concat(viewLocations);
            }
            return viewLocations;
        }
    }
}
