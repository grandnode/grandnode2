using Grand.Domain.Configuration;

namespace Widgets.Slider;

public class SliderWidgetSettings : ISettings
{
    public int DisplayOrder { get; set; }
    public IList<string> LimitedToStores { get; set; } = new List<string>();

    public IList<string> LimitedToGroups { get; set; } = new List<string>();
}