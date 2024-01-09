namespace Grand.Web.Common.View;

public interface IAreaViewFactory
{
    string AreaName { get; }
    IEnumerable<string> GetViewLocations(IEnumerable<string> viewLocations);
}