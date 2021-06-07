using Grand.Domain.Configuration;
using System.Collections.Generic;

namespace Widgets.Slider
{
    public class SliderWidgetSettings : ISettings
    {
        public SliderWidgetSettings()
        {
            LimitedToStores = new List<string>();
            LimitedToGroups = new List<string>();
        }
        public int DisplayOrder { get; set; }
        public IList<string> LimitedToStores { get; set; }

        public IList<string> LimitedToGroups { get; set; }
    }
}
