using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using Grand.Mediator;
using Widgets.Slider.Domain;
using Widgets.Slider.Services;

namespace Widgets.Slider.Events.Cache;

public class PictureSliderCacheEventHandler :
    INotificationHandler<EntityInserted<PictureSlider>>,
    INotificationHandler<EntityUpdated<PictureSlider>>,
    INotificationHandler<EntityDeleted<PictureSlider>>
{
    private readonly ICacheBase _cacheBase;

    public PictureSliderCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<PictureSlider> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(SliderService.SLIDERS_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<PictureSlider> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(SliderService.SLIDERS_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<PictureSlider> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(SliderService.SLIDERS_PATTERN_KEY);
    }
}
