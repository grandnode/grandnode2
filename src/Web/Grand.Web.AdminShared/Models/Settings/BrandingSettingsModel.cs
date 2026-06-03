using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.AdminShared.Models.Settings;

public class BrandingSettingsModel : BaseModel
{
    public string ActiveStore { get; set; }

    [GrandResourceDisplayName("Admin.Design.Branding.PrimaryColor")]
    public string PrimaryColor { get; set; }

    [GrandResourceDisplayName("Admin.Design.Branding.SecondaryColor")]
    public string SecondaryColor { get; set; }

    [GrandResourceDisplayName("Admin.Design.Branding.AccentColor")]
    public string AccentColor { get; set; }

    [GrandResourceDisplayName("Admin.Design.Branding.BackgroundColor")]
    public string BackgroundColor { get; set; }

    [GrandResourceDisplayName("Admin.Design.Branding.TextColor")]
    public string TextColor { get; set; }

    [UIHint("Picture")]
    [GrandResourceDisplayName("Admin.Design.Branding.Logo")]
    public string LogoPictureId { get; set; }

    [UIHint("Picture")]
    [GrandResourceDisplayName("Admin.Design.Branding.Favicon")]
    public string FaviconPictureId { get; set; }

    [UIHint("Picture")]
    [GrandResourceDisplayName("Admin.Design.Branding.Banner")]
    public string BannerPictureId { get; set; }
}
