using Grand.Business.Core.Interfaces.Storage;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Widgets.Slider;
using Widgets.Slider.Components;
using Widgets.Slider.Domain;
using Widgets.Slider.Services;

namespace Grand.Modules.Tests.Plugins.Widgets;

[TestClass]
public class WidgetSliderComponentTests
{
    private readonly WidgetSliderComponent _component;
    private readonly Mock<ISliderService> _sliders = new();

    public WidgetSliderComponentTests()
    {
        _sliders.Setup(x => x.GetPictureSliders(It.IsAny<SliderType>(), It.IsAny<string>()))
            .ReturnsAsync(new List<PictureSlider>());
        _component = new WidgetSliderComponent(new Mock<IPictureService>().Object, _sliders.Object,
            new Mock<IContextAccessor>().Object);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task EntityZonesWithoutAnEntity_AskForNoSlides(string entityId)
    {
        //a zone rendered without the id of its page's entity must not list the slides of every category
        foreach (var zone in new[] {
                     SliderWidgetDefaults.WidgetZoneCategoryPage, SliderWidgetDefaults.WidgetZoneCollectionPage,
                     SliderWidgetDefaults.WidgetZoneBrandPage
                 })
        {
            var result = await _component.InvokeAsync(zone, entityId);

            Assert.IsInstanceOfType<ContentViewComponentResult>(result);
        }

        _sliders.Verify(x => x.GetPictureSliders(It.IsAny<SliderType>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task CategoryZone_AsksForTheSlidesOfItsCategory()
    {
        await _component.InvokeAsync(SliderWidgetDefaults.WidgetZoneCategoryPage, "c1");

        _sliders.Verify(x => x.GetPictureSliders(SliderType.Category, "c1"), Times.Once);
    }
}
