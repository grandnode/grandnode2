namespace Grand.Web.Common.Themes;

public interface IThemeContextFactory
{
    IThemeContext GetThemeContext(string areaName);
}