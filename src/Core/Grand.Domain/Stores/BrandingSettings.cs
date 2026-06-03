using Grand.Domain.Configuration;

namespace Grand.Domain.Stores;

public class BrandingSettings : ISettings
{
    public string PrimaryColor { get; set; }
    public string SecondaryColor { get; set; }
    public string AccentColor { get; set; }
    public string BackgroundColor { get; set; }
    public string TextColor { get; set; }
    public string LogoPictureId { get; set; }
    public string FaviconPictureId { get; set; }
    public string BannerPictureId { get; set; }
}
