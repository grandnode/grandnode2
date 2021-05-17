using Grand.Business.Common.Interfaces.Security;
using Grand.Domain.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Widgets.Slider.Domain;

namespace Widgets.Slider.Services
{
    public partial class SliderService : ISliderService
    {
        #region Fields

        private readonly IRepository<PictureSlider> _reporistoryPictureSlider;
        private readonly IAclService _aclService;
        private readonly IWorkContext _workContext;
        private readonly ICacheBase _cacheBase;

        /// <summary>
        /// Key for sliders
        /// </summary>
        /// <remarks>
        /// {0} : Store id
        /// {1} : Slider type
        /// {2} : Object entry / categoryId || collectionId
        /// </remarks>
        public const string SLIDERS_MODEL_KEY = "Grand.slider-{0}-{1}-{2}";
        public const string SLIDERS_PATTERN_KEY = "Grand.slider";

        #endregion
        
        public SliderService(IRepository<PictureSlider> reporistoryPictureSlider,
            IWorkContext workContext, IAclService aclService,
            ICacheBase cacheManager)
        {
            _reporistoryPictureSlider = reporistoryPictureSlider;
            _workContext = workContext;
            _aclService = aclService;
            _cacheBase = cacheManager;
        }
        /// <summary>
        /// Delete a slider
        /// </summary>
        /// <param name="slider">Slider</param>
        public virtual async Task DeleteSlider(PictureSlider slide)
        {
            if (slide == null)
                throw new ArgumentNullException(nameof(slide));

            //clear cache
            await _cacheBase.RemoveByPrefix(SLIDERS_PATTERN_KEY);

            await _reporistoryPictureSlider.DeleteAsync(slide);
        }

        /// <summary>
        /// Gets all 
        /// </summary>
        /// <returns>Picture Sliders</returns>
        public virtual async Task<IList<PictureSlider>> GetPictureSliders()
        {
            return await Task.FromResult(_reporistoryPictureSlider.Table.OrderBy(x => x.SliderTypeId).ThenBy(x => x.DisplayOrder).ToList());
        }

        /// <summary>
        /// Gets by type 
        /// </summary>
        /// <returns>Picture Sliders</returns>
        public virtual async Task<IList<PictureSlider>> GetPictureSliders(SliderType sliderType, string objectEntry = "")
        {
            string cacheKey = string.Format(SLIDERS_MODEL_KEY, _workContext.CurrentStore.Id, sliderType.ToString(), objectEntry);
            return await _cacheBase.GetAsync(cacheKey, async () =>
            {
                var query = from s in _reporistoryPictureSlider.Table
                            where s.SliderTypeId == sliderType && s.Published
                            select s;

                if (!string.IsNullOrEmpty(objectEntry))
                    query = query.Where(x => x.ObjectEntry == objectEntry);

                var items = query.ToList();
                return await Task.FromResult(items.Where(c => _aclService.Authorize(c, _workContext.CurrentStore.Id)).ToList());
            });
        }


        /// <summary>
        /// Gets a tax rate
        /// </summary>
        /// <param name="slideId">Slide identifier</param>
        /// <returns>Tax rate</returns>
        public virtual Task<PictureSlider> GetById(string slideId)
        {
            return _reporistoryPictureSlider.FirstOrDefaultAsync(x => x.Id == slideId);
        }

        /// <summary>
        /// Inserts a slide
        /// </summary>
        /// <param name="slide">Picture Slider</param>
        public virtual async Task InsertPictureSlider(PictureSlider slide)
        {
            if (slide == null)
                throw new ArgumentNullException(nameof(slide));

            //clear cache
            await _cacheBase.RemoveByPrefix(SLIDERS_PATTERN_KEY);

            await _reporistoryPictureSlider.InsertAsync(slide);
        }

        /// <summary>
        /// Updates slide
        /// </summary>
        /// <param name="slide">Picture Slider</param>
        public virtual async Task UpdatePictureSlider(PictureSlider slide)
        {
            if (slide == null)
                throw new ArgumentNullException(nameof(slide));

            //clear cache
            await _cacheBase.RemoveByPrefix(SLIDERS_PATTERN_KEY);

            await _reporistoryPictureSlider.UpdateAsync(slide);
        }

        /// <summary>
        /// Delete slide
        /// </summary>
        /// <param name="slide">Picture Slider</param>
        public virtual async Task DeletePictureSlider(PictureSlider slide)
        {
            if (slide == null)
                throw new ArgumentNullException(nameof(slide));

            //clear cache
            await _cacheBase.RemoveByPrefix(SLIDERS_PATTERN_KEY);

            await _reporistoryPictureSlider.DeleteAsync(slide);
        }

    }
}
