using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.View
{
    public class ViewLocationExpander : IViewLocationExpander
    {
        public void PopulateValues(ViewLocationExpanderContext context)
        {
            
        }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            var viewFactory = context.ActionContext.HttpContext.RequestServices.GetRequiredService<IViewFactory>();
            viewFactory.GetViewPath(context.AreaName ?? "", ref viewLocations);
            return viewLocations;
        }
    }
}
