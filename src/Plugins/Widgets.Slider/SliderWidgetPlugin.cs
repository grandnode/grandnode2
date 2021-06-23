using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Data;
using Grand.Infrastructure.Plugins;
using Grand.SharedKernel.Extensions;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Widgets.Slider.Domain;

namespace Widgets.Slider
{
    /// <summary>
    /// PLugin
    /// </summary>
    public class SliderWidgetPlugin : BasePlugin, IPlugin
    {
        private readonly IPictureService _pictureService;
        private readonly IRepository<PictureSlider> _pictureSliderRepository;
        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;
        private readonly IDatabaseContext _databaseContext;

        public SliderWidgetPlugin(IPictureService pictureService,
            IRepository<PictureSlider> pictureSliderRepository,
            ITranslationService translationService,
            ILanguageService languageService,
            IDatabaseContext databaseContext)
        {
            _pictureService = pictureService;
            _pictureSliderRepository = pictureSliderRepository;
            _translationService = translationService;
            _languageService = languageService;
            _databaseContext = databaseContext;
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override async Task Install()
        {
            //Create index
            await _databaseContext.CreateIndex(_pictureSliderRepository, OrderBuilder<PictureSlider>.Create().Ascending(x => x.SliderTypeId).Ascending(x => x.DisplayOrder), "SliderTypeId_DisplayOrder");

            //pictures
            var sampleImagesPath = CommonPath.MapPath("Plugins/Widgets.Slider/Assets/slider/sample-images/");
            var byte1 = File.ReadAllBytes(sampleImagesPath + "banner1.png");
            var byte2 = File.ReadAllBytes(sampleImagesPath + "banner2.png");

            var pictureSlider1 = new PictureSlider()
            {
                DisplayOrder = 0,
                Link = "",
                Name = "Sample slider 1",
                FullWidth = true,
                Published = true,
                Description = "<div class=\"row slideRow justify-content-start\"><div class=\"col-lg-6 d-flex flex-column justify-content-center align-items-center\"><div><div class=\"animate-top animate__animated animate__backInDown\" >exclusive - modern - elegant</div><div class=\"animate-center-title animate__animated animate__backInLeft animate__delay-05s\">Smart watches</div><div class=\"animate-center-content animate__animated animate__backInLeft animate__delay-1s\">Go to collection and see more...</div><a href=\"/smartwatches\" class=\"animate-bottom btn btn-info animate__animated animate__backInUp animate__delay-15s\"> SHOP NOW </a></div></div></div>"
            };

            var pic1 = await _pictureService.InsertPicture(byte1, "image/png", "banner_1",reference: Grand.Domain.Common.Reference.Widget, objectId: pictureSlider1.Id, validateBinary: false);
            pictureSlider1.PictureId = pic1.Id;
            await _pictureSliderRepository.InsertAsync(pictureSlider1);


            var pictureSlider2 = new PictureSlider()
            {
                DisplayOrder = 1,
                Link = "https://grandnode.com",
                Name = "Sample slider 2",
                FullWidth = true,
                Published = true,
                Description = "<div class=\"row slideRow\"><div class=\"col-md-6 offset-md-6 col-12 offset-0 d-flex flex-column justify-content-center align-items-start px-0 pr-md-3\"><div class=\"slide-title text-dark animate__animated animate__fadeInRight animate__delay-05s\"><h2 class=\"mt-0\">Redmi Note 9</h2></div><div class=\"slide-content animate__animated animate__fadeInRight animate__delay-1s\"><p class=\"mb-0\"><span>Equipped with a high-performance octa-core processor <br/> with a maximum clock frequency of 2.0 GHz.</span></p></div><div class=\"slide-price animate__animated animate__fadeInRight animate__delay-15s d-inline-flex align-items-center justify-content-start w-100 mt-2\"><p class=\"actual\">$249.00</p><p class=\"old-price\">$399.00</p></div><div class=\"slide-button animate__animated animate__fadeInRight animate__delay-2s mt-3\"><a class=\"btn btn-outline-info\" href=\"/redmi-note-9\">BUY REDMI NOTE 9</a></div></div></div>",
            };
            var pic2 = await _pictureService.InsertPicture(byte2, "image/png", "banner_2", reference: Grand.Domain.Common.Reference.Widget, objectId: pictureSlider2.Id, validateBinary: false);
            pictureSlider2.PictureId = pic2.Id;

            await _pictureSliderRepository.InsertAsync(pictureSlider2);

            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Fields.DisplayOrder", "Display order");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Fields.LimitedToGroups", "Limited to groups");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Fields.LimitedToStores", "Limited to stores");


            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.FriendlyName", "Widget Slider");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Added", "Slider added");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Addnew", "Add new slider");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.AvailableStores", "Available stores");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.AvailableStores.Hint", "Select stores for which the slider will be shown.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Backtolist", "Back to list");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Category", "Category");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Category.Hint", "Select the category where slider should appear.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Category.Required", "Category is required");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Description", "Description");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Description.Hint", "Enter the description of the slider");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.DisplayOrder", "Display Order");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.DisplayOrder.Hint", "The slider display order. 1 represents the first item in the list.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Edit", "Edit slider");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Edited", "Slider edited");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Fields.Displayorder", "Display Order");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Fields.Link", "Link");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Fields.ObjectType", "Slider type");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Fields.Picture", "Picture");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Fields.Published", "Published");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Fields.Title", "Title");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.FullWidth", "Full width");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.FullWidth.hint", "Full width");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Info", "Info");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.LimitedToStores", "Limited to stores");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.LimitedToStores.Hint", "Determines whether the slider is available only at certain stores.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Link", "URL");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Link.Hint", "Enter URL. Leave empty if you don't want this picture to be clickable.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Manage", "Manage Bootstrap Slider");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Collection", "Collection");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Collection.Hint", "Select the collection where slider should appear.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Collection.Required", "Collection is required");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Brand", "Brand");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Brand.Hint", "Select the brand where slider should appear.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Brand.Required", "Brand is required");

            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Name", "Name");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Name.Hint", "Enter the name of the slider");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Name.Required", "Name is required");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Picture", "Picture");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Picture.Required", "Picture is required");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Published", "Published");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Published.Hint", "Specify it should be visible or not");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.SliderType", "Slider type");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.SliderType.Hint", "Choose the slider type. Home page, category or collection page.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Widgets.Slider.Stores", "Stores");


            await base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override async Task Uninstall()
        {

            //clear repository
            await _pictureSliderRepository.DeleteAsync(_pictureSliderRepository.Table.ToList());

            //locales
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Added");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Addnew");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.AvailableStores");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.AvailableStores.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Backtolist");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Category");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Category.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Category.Required");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Description");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Description.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.DisplayOrder");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.DisplayOrder.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Edit");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Edited");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Fields.Displayorder");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Fields.Link");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Fields.ObjectType");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Fields.Picture");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Fields.Published");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Fields.Title");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Info");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.LimitedToStores");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.LimitedToStores.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Link");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Link.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Manage");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Collection");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Collection.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Collection.Required");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Brand");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Brand.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Brand.Required");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Name");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Name.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Name.Required");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Picture");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Picture.Required");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Published");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Published.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.SliderType");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.SliderType.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Widgets.Slider.Stores");

            await base.Uninstall();
        }

        public override string ConfigurationUrl()
        {
            return SliderWidgetDefaults.ConfigurationUrl;
        }
    }
}
