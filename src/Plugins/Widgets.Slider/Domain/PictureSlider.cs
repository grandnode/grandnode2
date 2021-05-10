using Grand.Domain;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using System.Collections.Generic;

namespace Widgets.Slider.Domain
{
    public partial class PictureSlider : BaseEntity, ITranslationEntity, IStoreLinkEntity
    {
        public PictureSlider()
        {
            Stores = new List<string>();
            Locales = new List<TranslationEntity>();
        }
        public string PictureId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
        public int DisplayOrder { get; set; }
        public bool FullWidth { get; set; }
        public bool Published { get; set; }
        public int SliderTypeId { get; set; }
        public string ObjectEntry { get; set; }
        public SliderType SliderType {
            get {
                return (SliderType)SliderTypeId;
            }
            set {
                SliderTypeId = (int)value;
            }

        }
        public bool LimitedToStores { get; set; }
        public IList<string> Stores { get; set; }
        public IList<TranslationEntity> Locales { get; set; }
    }
}
