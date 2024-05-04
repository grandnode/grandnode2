namespace Grand.Web.Common.Themes;

public interface IThemeView
{
    string AreaName { get; }
    string ThemeName { get; }
    public ThemeInfo ThemeInfo { get; }
    IEnumerable<string> GetViewLocations();
}

public record ThemeInfo(string Title, string PreviewImageUrl, string PreviewText, bool SupportRtl);