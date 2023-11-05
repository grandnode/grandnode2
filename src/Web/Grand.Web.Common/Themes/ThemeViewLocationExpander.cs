using Microsoft.AspNetCore.Mvc.Razor;

namespace Grand.Web.Common.Themes
{
    public class ThemeViewLocationExpander : IViewLocationExpander
    {
        private const string ThemeKey = "Theme";

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            if (!string.IsNullOrEmpty(context.AreaName)) return;
            
            var themeContext = (IThemeContext)context.ActionContext.HttpContext.RequestServices.GetService(typeof(IThemeContext));
            context.Values[ThemeKey] = themeContext?.WorkingThemeName;
        }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            if (string.IsNullOrEmpty(context.AreaName) && context.Values.TryGetValue(ThemeKey, out var theme))
            {
                viewLocations = new[] {
                        $"/Themes/{theme}/Views/{{1}}/{{0}}.cshtml",
                        $"/Themes/{theme}/Views/Shared/{{0}}.cshtml"
                    }
                    .Concat(viewLocations);
            }
            return viewLocations;
        }
    }
}
