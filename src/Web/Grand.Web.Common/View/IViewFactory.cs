namespace Grand.Web.Common.View;

public interface IViewFactory
{
    void GetViewPath(
        string areaName,
        ref IEnumerable<string> viewLocations);
}