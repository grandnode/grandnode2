using Grand.Business.Marketing.Interfaces.Newsletters;
using Grand.Infrastructure.Extensions;
using Grand.Domain.Data;
using Grand.Domain.Messages;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Services.Newsteletters
{
    public partial class NewsletterCategoryService : INewsletterCategoryService
    {
        #region Fields

        private readonly IRepository<NewsletterCategory> _newsletterCategoryRepository;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        public NewsletterCategoryService(IRepository<NewsletterCategory> newsletterCategoryRepository, IMediator mediator)
        {
            _newsletterCategoryRepository = newsletterCategoryRepository;
            _mediator = mediator;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Inserts a newsletter category
        /// </summary>
        /// <param name="newslettercategory">NewsletterCategory</param>        
        public virtual async Task InsertNewsletterCategory(NewsletterCategory newslettercategory)
        {
            if (newslettercategory == null)
                throw new ArgumentNullException(nameof(newslettercategory));

            await _newsletterCategoryRepository.InsertAsync(newslettercategory);

            //event notification
            await _mediator.EntityInserted(newslettercategory);
        }

        /// <summary>
        /// Updates a newsletter category
        /// </summary>
        /// <param name="newslettercategory">NewsletterCategory</param>
        public virtual async Task UpdateNewsletterCategory(NewsletterCategory newslettercategory)
        {
            if (newslettercategory == null)
                throw new ArgumentNullException(nameof(newslettercategory));

            await _newsletterCategoryRepository.UpdateAsync(newslettercategory);

            //event notification
            await _mediator.EntityUpdated(newslettercategory);

        }

        /// <summary>
        /// Deleted a newsletter category
        /// </summary>
        /// <param name="newslettercategory">NewsletterCategory</param>
        public virtual async Task DeleteNewsletterCategory(NewsletterCategory newslettercategory)
        {
            if (newslettercategory == null)
                throw new ArgumentNullException(nameof(newslettercategory));

            await _newsletterCategoryRepository.DeleteAsync(newslettercategory);

            //event notification
            await _mediator.EntityDeleted(newslettercategory);

        }

        /// <summary>
        /// Gets a newsletter by category
        /// </summary>
        /// <param name="id">newsletter category</param>
        /// <returns>Banner</returns>
        public virtual Task<NewsletterCategory> GetNewsletterCategoryById(string id)
        {
            return _newsletterCategoryRepository.GetByIdAsync(id);
        }

        /// <summary>
        /// Gets all newsletter categories
        /// </summary>
        /// <returns>NewsletterCategories</returns>
        public virtual async Task<IList<NewsletterCategory>> GetAllNewsletterCategory()
        {
            return await Task.FromResult(_newsletterCategoryRepository.Table.ToList());
        }

        /// <summary>
        /// Gets all newsletter categories by store
        /// </summary>
        /// <returns>NewsletterCategories</returns>
        public virtual async Task<IList<NewsletterCategory>> GetNewsletterCategoriesByStore(string storeId)
        {
            var query = from p in _newsletterCategoryRepository.Table
                        where !p.LimitedToStores || p.Stores.Contains(storeId)
                        orderby p.DisplayOrder
                        select p;
            return await Task.FromResult(query.ToList());
        }
        #endregion
    }
}
