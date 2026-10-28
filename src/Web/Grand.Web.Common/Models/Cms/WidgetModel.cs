using Grand.Infrastructure.Models;

//the namespace stays from Grand.Web.Models: storefront and theme _ViewImports import it
namespace Grand.Web.Models.Cms;

public class WidgetModel : BaseModel
{
    public string WidgetZone { get; set; }
    public string ViewComponentName { get; set; }
    public object AdditionalData { get; set; }
}
