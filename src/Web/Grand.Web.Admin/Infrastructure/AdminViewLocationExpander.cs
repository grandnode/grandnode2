using Grand.Web.Common.Themes;
using Microsoft.AspNetCore.Mvc.Razor;

namespace Grand.Web.Admin.Infrastructure
{
    public class AdminViewLocationExpander : IViewLocationExpander
    {
        private const string AreaAdminKey = "AdminTheme";


        public void PopulateValues(ViewLocationExpanderContext context)
        {
            if (!(context.AreaName?.Equals("Admin") ?? false)) return;
            
            var themeContext = (IThemeContext)context.ActionContext.HttpContext.RequestServices.GetService(typeof(IThemeContext));
            context.Values[AreaAdminKey] = themeContext?.AdminAreaThemeName;
        }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            
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
