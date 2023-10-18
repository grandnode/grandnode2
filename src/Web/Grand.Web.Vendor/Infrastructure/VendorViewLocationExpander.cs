using Microsoft.AspNetCore.Mvc.Razor;

namespace Grand.Web.Vendor.Infrastructure
{
    public class VendorViewLocationExpander : IViewLocationExpander
    {
        private const string AreaVendorKey = "Vendor";


        public void PopulateValues(ViewLocationExpanderContext context)
        {
            if (!(context.AreaName?.Equals("Vendor") ?? false)) return;
            
            context.Values[AreaVendorKey] = AreaVendorKey;
        }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            
            if ((context.AreaName?.Equals("Vendor") ?? false) && context.Values.TryGetValue(AreaVendorKey, out var vendorTheme))
            {
                viewLocations = new[] {
                        $"/Areas/{{2}}/Themes/{vendorTheme}/Views/{{1}}/{{0}}.cshtml",
                        $"/Areas/{{2}}/Themes/{vendorTheme}/Views/Shared/{{0}}.cshtml"
                    }
                    .Concat(viewLocations);
            }
            return viewLocations;
        }
    }
}
