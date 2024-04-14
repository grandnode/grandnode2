#nullable enable
namespace Grand.Web.Common.View;

public class ViewFactory : IViewFactory
{
    private readonly IDictionary<string, IAreaViewFactory> _areaFactories;

    public ViewFactory(IEnumerable<IAreaViewFactory> areaFactories)
    {
        _areaFactories = areaFactories.ToDictionary(f => f.AreaName, f => f);
    }

    public void GetViewPath(string areaName, ref IEnumerable<string> viewLocations)
    {
        if (_areaFactories.TryGetValue(areaName, out var areaFactory))
        {
            viewLocations = areaFactory.GetViewLocations(viewLocations);
            return;
        }

        viewLocations = new List<string> {
            "/Views/{1}/{0}.cshtml",
            "/Views/Shared/{0}.cshtml"
        };
    }
}