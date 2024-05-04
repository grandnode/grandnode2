using Grand.Infrastructure.Models;

namespace Grand.Web.Vendor.Models.Cms;

public class VendorWidgetModel : BaseModel
{
    public string WidgetZone { get; set; }
    public string ViewComponentName { get; set; }
    public object AdditionalData { get; set; }
}