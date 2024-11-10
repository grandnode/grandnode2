using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Data;
using Grand.Domain.Common;
using Grand.Infrastructure.Plugins;
using Microsoft.AspNetCore.Hosting;
using Widgets.Slider.Domain;

namespace Widgets.Slider;

/// <summary>
///     Plugin
/// </summary>
public class SliderWidgetPlugin(
    IPictureService pictureService,
    IRepository<PictureSlider> pictureSliderRepository,
    IPluginTranslateResource pluginTranslateResource,
    IWebHostEnvironment webHostEnvironment)
    : BasePlugin, IPlugin
{

    /// <summary>
    ///     Install plugin
    /// </summary>
    public override async Task Install()
    {
        //pictures
        var sampleImagesPath = Path.Combine(webHostEnvironment.ContentRootPath, "Plugins/Widgets.Slider/Assets/slider/sample-images/");
        var byte1 = await File.ReadAllBytesAsync(sampleImagesPath + "banner1.png");
        var byte2 = await File.ReadAllBytesAsync(sampleImagesPath + "banner2.png");

        var pictureSlider1 = new PictureSlider {
            DisplayOrder = 0,
            Link = "",
            Name = "Sample slider 1",
            FullWidth = true,
            Published = true,
            Description =
                "<div class=\"row slideRow justify-content-start\"><div class=\"col-lg-6 d-flex flex-column justify-content-center align-items-center\"><div><div class=\"animate-top animate__animated animate__backInDown\" >exclusive - modern - elegant</div><div class=\"animate-center-title animate__animated animate__backInLeft animate__delay-05s\">Smart watches</div><div class=\"animate-center-content animate__animated animate__backInLeft animate__delay-1s\">Go to collection and see more...</div><a href=\"/smartwatches\" class=\"animate-bottom btn btn-info animate__animated animate__backInUp animate__delay-15s\"> SHOP NOW </a></div></div></div>"
        };

        var pic1 = await pictureService.InsertPicture(byte1, "image/png", "banner_1", reference: Reference.Widget,
            objectId: pictureSlider1.Id, validateBinary: false);
        pictureSlider1.PictureId = pic1.Id;
        await pictureSliderRepository.InsertAsync(pictureSlider1);


        var pictureSlider2 = new PictureSlider {
            DisplayOrder = 1,
            Link = "https://grandnode.com",
            Name = "Sample slider 2",
            FullWidth = true,
            Published = true,
            Description =
                "<div class=\"row slideRow\"><div class=\"col-md-6 offset-md-6 col-12 offset-0 d-flex flex-column justify-content-center align-items-start px-0 pr-md-3\"><div class=\"slide-title text-dark animate__animated animate__fadeInRight animate__delay-05s\"><h2 class=\"mt-0\">Redmi Note 9</h2></div><div class=\"slide-content animate__animated animate__fadeInRight animate__delay-1s\"><p class=\"mb-0\"><span>Equipped with a high-performance octa-core processor <br/> with a maximum clock frequency of 2.0 GHz.</span></p></div><div class=\"slide-price animate__animated animate__fadeInRight animate__delay-15s d-inline-flex align-items-center justify-content-start w-100 mt-2\"><p class=\"actual\">$249.00</p><p class=\"old-price\">$399.00</p></div><div class=\"slide-button animate__animated animate__fadeInRight animate__delay-2s mt-3\"><a class=\"btn btn-outline-info\" href=\"/redmi-note-9\">BUY REDMI NOTE 9</a></div></div></div>"
        };
        var pic2 = await pictureService.InsertPicture(byte2, "image/png", "banner_2", reference: Reference.Widget,
            objectId: pictureSlider2.Id, validateBinary: false);
        pictureSlider2.PictureId = pic2.Id;

        await pictureSliderRepository.InsertAsync(pictureSlider2);

        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Fields.DisplayOrder", "Display order");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Fields.LimitedToGroups", "Limited to groups");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Fields.LimitedToStores", "Limited to stores");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.FriendlyName", "Widget Slider");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource( "Widgets.Slider.Added", "Slider added");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource( "Widgets.Slider.Addnew", "Add new slider");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.AvailableStores", "Available stores");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.AvailableStores.Hint", "Select stores for which the slider will be shown.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Backtolist", "Back to list");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource( "Widgets.Slider.Category", "Category");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Category.Hint", "Select the category where slider should appear.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Category.Required", "Category is required");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Description", "Description");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Description.Hint", "Enter the description of the slider");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.DisplayOrder", "Display Order");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.DisplayOrder.Hint", "The slider display order. 1 represents the first item in the list.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource( "Widgets.Slider.Edit", "Edit slider");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource( "Widgets.Slider.Edited", "Slider edited");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Fields.Displayorder", "Display Order");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Fields.Link", "Link");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Fields.ObjectType", "Slider type");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Fields.Picture", "Picture");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Fields.Published", "Published");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Fields.Title", "Title");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource( "Widgets.Slider.FullWidth", "Full width");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.FullWidth.hint", "Full width");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource( "Widgets.Slider.Info", "Info");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.LimitedToStores", "Limited to stores");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.LimitedToStores.Hint", "Determines whether the slider is available only at certain stores.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource( "Widgets.Slider.Link", "URL");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource( "Widgets.Slider.Link.Hint", "Enter URL. Leave empty if you don't want this picture to be clickable.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource( "Widgets.Slider.Manage", "Manage Bootstrap Slider");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Collection", "Collection");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Collection.Hint", "Select the collection where slider should appear.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Collection.Required", "Collection is required");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource( "Widgets.Slider.Brand", "Brand");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Brand.Hint", "Select the brand where slider should appear.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Brand.Required", "Brand is required");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource( "Widgets.Slider.Name", "Name");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource( "Widgets.Slider.Name.Hint", "Enter the name of the slider");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Name.Required", "Name is required");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource( "Widgets.Slider.Picture", "Picture");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Picture.Required", "Picture is required");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource( "Widgets.Slider.Published", "Published");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.Published.Hint", "Specify it should be visible or not");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.SliderType", "Slider type");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Widgets.Slider.SliderType.Hint", "Choose the slider type. Home page, category or collection page.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource( "Widgets.Slider.Stores", "Stores");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource( "Widgets.Slider.StartDate", "Start Date");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource( "Widgets.Slider.EndDate", "End Date");

        await base.Install();
    }

    /// <summary>
    ///     Uninstall plugin
    /// </summary>
    public override async Task Uninstall()
    {
        //clear repository
        await pictureSliderRepository.DeleteAsync(pictureSliderRepository.Table.ToList());

        //locales
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.Added");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.Addnew");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.AvailableStores");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.AvailableStores.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.Backtolist");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.Category");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.Category.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.Category.Required");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.Description");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.Description.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.DisplayOrder");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.DisplayOrder.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.Edit");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.Edited");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.Fields.Displayorder");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.Fields.Link");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.Fields.ObjectType");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.Fields.Picture");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.Fields.Published");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.Fields.Title");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.Info");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.LimitedToStores");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.LimitedToStores.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.Link");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.Link.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.Manage");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.Collection");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.Collection.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.Collection.Required");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.Brand");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.Brand.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.Brand.Required");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.Name");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.Name.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.Name.Required");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.Picture");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.Picture.Required");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.Published");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.Published.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.SliderType");
        await pluginTranslateResource.DeletePluginTranslationResource("Widgets.Slider.SliderType.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.Stores");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.StartDate");
        await pluginTranslateResource.DeletePluginTranslationResource( "Widgets.Slider.EndDate");

        await base.Uninstall();
    }

    public override string ConfigurationUrl()
    {
        return SliderWidgetDefaults.ConfigurationUrl;
    }
}