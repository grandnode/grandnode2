using Grand.Infrastructure.Models;

namespace Grand.Web.AdminShared.Models.Cms;

public class AdminWidgetModel : BaseModel
{
    public string? WidgetZone { get; set; }
    public required string ViewComponentName { get; set; }
    public object? AdditionalData { get; set; }
}