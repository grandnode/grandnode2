using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Widgets.Slider.Domain;

namespace Widgets.Slider.Services;

public class SliderService : ISliderService
{
    public SliderService(IRepository<PictureSlider> repositoryPictureSlider,
        IWorkContext workContext, IAclService aclService,
        ICacheBase cacheManager)
    {
        _repositoryPictureSlider = repositoryPictureSlider;
        _workContext = workContext;
        _aclService = aclService;
        _cacheBase = cacheManager;
    }

    /// <summary>
    ///     Delete a slider
    /// </summary>
    /// <param name="slider">Slider</param>
    public virtual async Task DeleteSlider(PictureSlider slider)
    {
        ArgumentNullException.ThrowIfNull(slider);

        //clear cache
        await _cacheBase.RemoveByPrefix(SLIDERS_PATTERN_KEY);

        await _repositoryPictureSlider.DeleteAsync(slider);
    }

    /// <summary>
    ///     Gets all
    /// </summary>
    /// <returns>Picture Sliders</returns>
    public virtual async Task<IList<PictureSlider>> GetPictureSliders()
    {
        return await Task.FromResult(_repositoryPictureSlider.Table.OrderBy(x => x.SliderTypeId)
            .ThenBy(x => x.DisplayOrder).ToList());
    }

    /// <summary>
    ///     Gets by type
    /// </summary>
    /// <returns>Picture Sliders</returns>
    public virtual async Task<IList<PictureSlider>> GetPictureSliders(SliderType sliderType, string objectEntry = "")
    {
        var cacheKey = string.Format(SLIDERS_MODEL_KEY, _workContext.CurrentStore.Id, sliderType.ToString(),
            objectEntry);
        return await _cacheBase.GetAsync(cacheKey, async () =>
        {
            var query = from s in _repositoryPictureSlider.Table
                where s.SliderTypeId == sliderType && s.Published
                select s;

            if (!string.IsNullOrEmpty(objectEntry))
                query = query.Where(x => x.ObjectEntry == objectEntry);

            var items = query.ToList();
            return await Task.FromResult(items.Where(c => _aclService.Authorize(c, _workContext.CurrentStore.Id))
                .ToList());
        });
    }


    /// <summary>
    ///     Gets a tax rate
    /// </summary>
    /// <param name="slideId">Slide identifier</param>
    /// <returns>Tax rate</returns>
    public virtual Task<PictureSlider> GetById(string slideId)
    {
        return _repositoryPictureSlider.GetByIdAsync(slideId);
    }

    /// <summary>
    ///     Inserts a slide
    /// </summary>
    /// <param name="slide">Picture Slider</param>
    public virtual async Task InsertPictureSlider(PictureSlider slide)
    {
        ArgumentNullException.ThrowIfNull(slide);

        //clear cache
        await _cacheBase.RemoveByPrefix(SLIDERS_PATTERN_KEY);

        await _repositoryPictureSlider.InsertAsync(slide);
    }

    /// <summary>
    ///     Updates slide
    /// </summary>
    /// <param name="slide">Picture Slider</param>
    public virtual async Task UpdatePictureSlider(PictureSlider slide)
    {
        ArgumentNullException.ThrowIfNull(slide);

        //clear cache
        await _cacheBase.RemoveByPrefix(SLIDERS_PATTERN_KEY);

        await _repositoryPictureSlider.UpdateAsync(slide);
    }

    /// <summary>
    ///     Delete slide
    /// </summary>
    /// <param name="slide">Picture Slider</param>
    public virtual async Task DeletePictureSlider(PictureSlider slide)
    {
        ArgumentNullException.ThrowIfNull(slide);

        //clear cache
        await _cacheBase.RemoveByPrefix(SLIDERS_PATTERN_KEY);

        await _repositoryPictureSlider.DeleteAsync(slide);
    }

    #region Fields

    private readonly IRepository<PictureSlider> _repositoryPictureSlider;
    private readonly IAclService _aclService;
    private readonly IWorkContext _workContext;
    private readonly ICacheBase _cacheBase;

    /// <summary>
    ///     Key for sliders
    /// </summary>
    /// <remarks>
    ///     {0} : Store id
    ///     {1} : Slider type
    ///     {2} : Object entry / categoryId || collectionId
    /// </remarks>
    private const string SLIDERS_MODEL_KEY = "Grand.slider-{0}-{1}-{2}";

    private const string SLIDERS_PATTERN_KEY = "Grand.slider";

    #endregion
}