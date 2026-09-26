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
        //sample slides: wide photos that link to the sample store's departments, collection and marketplace
        var sampleImagesPath = Path.Combine(webHostEnvironment.ContentRootPath, "Plugins/Widgets.Slider/Assets/slider/sample-images/");
        var slides = new[] {
            (Name: "Slow Morning", File: "slide_slow_morning.jpg", Kicker: "The Slow Morning collection",
                Title: "Kettle on, phone off.", Text: "Everything for an unhurried first hour of the day.",
                Button: "Shop the collection", Url: "/slow-morning"),
            (Name: "Outdoor and Active", File: "slide_outdoor_active.jpg", Kicker: "New department",
                Title: "Outdoor & Active is here", Text: "Tents, trail shoes and cycling gear, with 15% off the whole department.",
                Button: "Explore the outdoors", Url: "/outdoor-active"),
            (Name: "Maker marketplace", File: "slide_marketplace.jpg", Kicker: "Our marketplace",
                Title: "Made by hand", Text: "Independent makers now sell alongside our own range.",
                Button: "Meet the makers", Url: "/nordic-craft-collective")
        };

        for (var i = 0; i < slides.Length; i++)
        {
            var slide = slides[i];
            var pictureSlider = new PictureSlider {
                DisplayOrder = i,
                Link = "",
                Name = slide.Name,
                FullWidth = true,
                Published = true,
                Description =
                    "<div class=\"row slideRow justify-content-start\"><div class=\"col-lg-6 d-flex flex-column justify-content-center align-items-center\"><div>" +
                    $"<div class=\"animate-top animate__animated animate__backInDown\">{slide.Kicker}</div>" +
                    $"<div class=\"animate-center-title animate__animated animate__backInLeft animate__delay-05s\">{slide.Title}</div>" +
                    $"<div class=\"animate-center-content animate__animated animate__backInLeft animate__delay-1s\">{slide.Text}</div>" +
                    $"<a href=\"{slide.Url}\" class=\"animate-bottom btn btn-info animate__animated animate__backInUp animate__delay-15s\">{slide.Button}</a>" +
                    "</div></div></div>"
            };

            var bytes = await File.ReadAllBytesAsync(sampleImagesPath + slide.File);
            var picture = await pictureService.InsertPicture(bytes, "image/jpeg", Path.GetFileNameWithoutExtension(slide.File),
                reference: Reference.Widget, objectId: pictureSlider.Id, validateBinary: false);
            pictureSlider.PictureId = picture.Id;
            await pictureSliderRepository.InsertAsync(pictureSlider);
        }

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