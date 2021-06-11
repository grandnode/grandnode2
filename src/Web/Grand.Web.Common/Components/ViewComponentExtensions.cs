using Grand.Web.Common.Themes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.Components
{
    public static class ViewComponentExtensions
    {
        public static string GetViewPath(this ViewComponent viewComponent, string viewName = "Default")
        {
            var themeContext = viewComponent.HttpContext.RequestServices.GetService<IThemeContext>();
            var theme = themeContext.WorkingThemeName;

            var viewPath = $"Views/Shared/Components/{viewComponent.ViewComponentContext.ViewComponentDescriptor.ShortName}/{viewName}.cshtml";
            var themeViewPath = $"/Themes/{theme}{viewPath}";
            var viewEngine = viewComponent.ViewContext.HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();
            var result = viewEngine.GetView("", themeViewPath, isMainPage: false);
            if (result.Success)
            {
                viewPath = themeViewPath;
            }

            return viewPath;
        }
    }
}
