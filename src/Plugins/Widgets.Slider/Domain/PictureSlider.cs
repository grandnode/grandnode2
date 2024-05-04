using Grand.Domain;
using Grand.Domain.Localization;
using Grand.Domain.Stores;

namespace Widgets.Slider.Domain;

public class PictureSlider : BaseEntity, ITranslationEntity, IStoreLinkEntity
{
    public string PictureId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Link { get; set; }
    public int DisplayOrder { get; set; }
    public bool FullWidth { get; set; }
    public bool Published { get; set; }
    public string ObjectEntry { get; set; }
    public SliderType SliderTypeId { get; set; }
    public DateTime? StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }
    public bool LimitedToStores { get; set; }
    public IList<string> Stores { get; set; } = new List<string>();
    public IList<TranslationEntity> Locales { get; set; } = new List<TranslationEntity>();
}