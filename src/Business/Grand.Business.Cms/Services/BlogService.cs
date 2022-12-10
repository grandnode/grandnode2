using Grand.Business.Core.Interfaces.Cms;
using Grand.Domain;
using Grand.Domain.Blogs;
using Grand.Domain.Data;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel.Extensions;
using MediatR;

namespace Grand.Business.Cms.Services
{
    /// <summary>
    /// Blog service
    /// </summary>
    public class BlogService : IBlogService
    {
        #region Fields

        private readonly IRepository<BlogPost> _blogPostRepository;
        private readonly IRepository<BlogComment> _blogCommentRepository;
        private readonly IRepository<BlogCategory> _blogCategoryRepository;
        private readonly IRepository<BlogProduct> _blogProductRepository;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        public BlogService(IRepository<BlogPost> blogPostRepository,
            IRepository<BlogComment> blogCommentRepository,
            IRepository<BlogCategory> blogCategoryRepository,
            IRepository<BlogProduct> blogProductRepository,
            IMediator mediator)
        {
            _blogPostRepository = blogPostRepository;
            _blogCommentRepository = blogCommentRepository;
            _blogCategoryRepository = blogCategoryRepository;
            _blogProductRepository = blogProductRepository;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a blog post
        /// </summary>
        /// <param name="blogPostId">Blog post identifier</param>
        /// <returns>Blog post</returns>
        public virtual Task<BlogPost> GetBlogPostById(string blogPostId)
        {
            return _blogPostRepository.GetByIdAsync(blogPostId);
        }

        /// <summary>
        /// Gets all blog posts
        /// </summary>
        /// <param name="storeId">The store identifier; pass "" to load all records</param>
        /// <param name="dateFrom">Filter by created date; null if you want to get all records</param>
        /// <param name="dateTo">Filter by created date; null if you want to get all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <param name="tag">Tag</param>
        /// <param name="blogPostName">Blog post name</param>
        /// <param name="categoryId">Category ident</param>
        /// <returns>Blog posts</returns>
        public virtual async Task<IPagedList<BlogPost>> GetAllBlogPosts(string storeId = "",
            DateTime? dateFrom = null, DateTime? dateTo = null,
            int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false, string tag = null, string blogPostName = "", string categoryId = "")
        {

            var query = from p in _blogPostRepository.Table
                        select p;

            if (!string.IsNullOrEmpty(categoryId))
            {
                var category = _blogCategoryRepository.Table.FirstOrDefault(x => x.Id == categoryId);
                if (category != null)
                {
                    var postsIds = category.BlogPosts.Select(x => x.BlogPostId);
                    query = query.Where(x => postsIds.Contains(x.Id));
                }
            }

            if (!string.IsNullOrWhiteSpace(blogPostName))
            {
                query = query.Where
                    (b => (b.Title != null && b.Title.ToLower().Contains(blogPostName.ToLower())) ||
                    (b.BodyOverview != null && b.BodyOverview.ToLower().Contains(blogPostName.ToLower())));
            }
            if (dateFrom.HasValue)
                query = query.Where(b => dateFrom.Value <= b.CreatedOnUtc);
            if (dateTo.HasValue)
                query = query.Where(b => dateTo.Value >= b.CreatedOnUtc);

            if (!showHidden)
            {
                var utcNow = DateTime.UtcNow;
                query = query.Where(b => !b.StartDateUtc.HasValue || b.StartDateUtc <= utcNow);
                query = query.Where(b => !b.EndDateUtc.HasValue || b.EndDateUtc >= utcNow);
            }

            if (!string.IsNullOrEmpty(storeId) && !CommonHelper.IgnoreStoreLimitations)
            {
                query = query.Where(b => b.Stores.Contains(storeId) || !b.LimitedToStores);
            }
            if (!string.IsNullOrEmpty(tag))
            {
                query = query.Where(x => x.Tags.Contains(tag));
            }

            query = query.OrderByDescending(b => b.CreatedOnUtc);

            return await PagedList<BlogPost>.Create(query, pageIndex, pageSize);

        }


        /// <summary>
        /// Gets all blog posts
        /// </summary>
        /// <param name="storeId">The store identifier; pass "" to load all records</param>
        /// <param name="tag">Tag</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Blog posts</returns>
        public virtual async Task<IPagedList<BlogPost>> GetAllBlogPostsByTag(string storeId = "",
            string tag = "",
            int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
        {
            tag = tag.Trim();

            //we load all records and only then filter them by tag
            var blogPostsAll = await GetAllBlogPosts(storeId: storeId, showHidden: showHidden, tag: tag);
            var taggedBlogPosts = (from blogPost in blogPostsAll let tags = blogPost.ParseTags() where !string.IsNullOrEmpty(tags.FirstOrDefault(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase))) select blogPost).ToList();

            //server-side paging
            return new PagedList<BlogPost>(taggedBlogPosts, pageIndex, pageSize);
        }

        /// <summary>
        /// Gets all blog post tags
        /// </summary>
        /// <param name="storeId">The store identifier; pass "" to load all records</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Blog post tags</returns>
        public virtual async Task<IList<BlogPostTag>> GetAllBlogPostTags(string storeId, bool showHidden = false)
        {
            var blogPostTags = new List<BlogPostTag>();

            var blogPosts = await GetAllBlogPosts(storeId: storeId, showHidden: showHidden);
            foreach (var blogPost in blogPosts)
            {
                var tags = blogPost.ParseTags();
                foreach (var tag in tags)
                {
                    var foundBlogPostTag = blogPostTags.Find(bpt => bpt.Name.Equals(tag, StringComparison.OrdinalIgnoreCase));
                    if (foundBlogPostTag == null)
                    {
                        foundBlogPostTag = new BlogPostTag
                        {
                            Name = tag,
                            BlogPostCount = 1
                        };
                        blogPostTags.Add(foundBlogPostTag);
                    }
                    else
                        foundBlogPostTag.BlogPostCount++;
                }
            }

            return blogPostTags;
        }

        /// <summary>
        /// Inserts an blog post
        /// </summary>
        /// <param name="blogPost">Blog post</param>
        public virtual async Task InsertBlogPost(BlogPost blogPost)
        {
            if (blogPost == null)
                throw new ArgumentNullException(nameof(blogPost));

            await _blogPostRepository.InsertAsync(blogPost);

            //event notification
            await _mediator.EntityInserted(blogPost);
        }

        /// <summary>
        /// Updates the blog post
        /// </summary>
        /// <param name="blogPost">Blog post</param>
        public virtual async Task UpdateBlogPost(BlogPost blogPost)
        {
            if (blogPost == null)
                throw new ArgumentNullException(nameof(blogPost));

            await _blogPostRepository.UpdateAsync(blogPost);

            //event notification
            await _mediator.EntityUpdated(blogPost);
        }
        /// <summary>
        /// Deletes a blog post
        /// </summary>
        /// <param name="blogPost">Blog post</param>
        public virtual async Task DeleteBlogPost(BlogPost blogPost)
        {
            if (blogPost == null)
                throw new ArgumentNullException(nameof(blogPost));

            await _blogPostRepository.DeleteAsync(blogPost);

            //event notification
            await _mediator.EntityDeleted(blogPost);
        }

        /// <summary>
        /// Gets all comments
        /// </summary>
        /// <param name="customerId">Customer identifier; "" to load all records</param>
        /// <param name="storeId">Store ident</param>
        /// <returns>Comments</returns>
        public virtual async Task<IList<BlogComment>> GetAllComments(string customerId, string storeId)
        {
            var query = from c in _blogCommentRepository.Table
                        orderby c.CreatedOnUtc
                        where (customerId == "" || c.CustomerId == customerId) && (storeId == "" || c.StoreId == storeId)
                        select c;

            return await Task.FromResult(query.ToList());
        }

        /// <summary>
        /// Gets a blog comment
        /// </summary>
        /// <param name="blogCommentId">Blog comment identifier</param>
        /// <returns>Blog comment</returns>
        public virtual Task<BlogComment> GetBlogCommentById(string blogCommentId)
        {
            return _blogCommentRepository.GetByIdAsync(blogCommentId);
        }

        public virtual async Task<IList<BlogComment>> GetBlogCommentsByBlogPostId(string blogPostId)
        {
            var query = from c in _blogCommentRepository.Table
                        where c.BlogPostId == blogPostId
                        orderby c.CreatedOnUtc
                        select c;

            return await Task.FromResult(query.ToList());
        }
        /// <summary>
        /// Get blog comments by identifiers
        /// </summary>
        /// <param name="commentIds">Blog comment identifiers</param>
        /// <returns>Blog comments</returns>
        public virtual async Task<IList<BlogComment>> GetBlogCommentsByIds(string[] commentIds)
        {
            if (commentIds == null || commentIds.Length == 0)
                return new List<BlogComment>();

            var query = from bc in _blogCommentRepository.Table
                        where commentIds.Contains(bc.Id)
                        select bc;
            var comments = query.ToList();
            //sort by passed identifiers
            var sortedComments = commentIds.Select(id => comments.Find(x => x.Id == id)).Where(comment => comment != null).ToList();
            return await Task.FromResult(sortedComments);
        }

        /// <summary>
        /// Inserts a blog post comment
        /// </summary>
        /// <param name="blogComment">Blog post comment</param>
        public virtual async Task InsertBlogComment(BlogComment blogComment)
        {
            if (blogComment == null)
                throw new ArgumentNullException(nameof(blogComment));

            await _blogCommentRepository.InsertAsync(blogComment);

            //event notification
            await _mediator.EntityInserted(blogComment);
        }

        public virtual async Task DeleteBlogComment(BlogComment blogComment)
        {
            if (blogComment == null)
                throw new ArgumentNullException(nameof(blogComment));

            await _blogCommentRepository.DeleteAsync(blogComment);
        }

        #region Blog category

        /// <summary>
        /// Get category by id
        /// </summary>
        /// <param name="blogCategoryId">Blog category id</param>
        /// <returns></returns>
        public virtual Task<BlogCategory> GetBlogCategoryById(string blogCategoryId)
        {
            return _blogCategoryRepository.GetByIdAsync(blogCategoryId);
        }

        /// <summary>
        /// Get categories by post id
        /// </summary>
        /// <param name="blogPostId">Blog post id</param>
        /// <returns></returns>
        public virtual async Task<IList<BlogCategory>> GetBlogCategoryByPostId(string blogPostId)
        {
            return await Task.FromResult(_blogCategoryRepository.Table.Where(x => x.BlogPosts.Any(x => x.BlogPostId == blogPostId)).ToList());
        }

        /// <summary>
        /// Get category by se-name
        /// </summary>
        /// <param name="blogCategorySeName">Blog category se-name</param>
        /// <returns></returns>
        public virtual async Task<BlogCategory> GetBlogCategoryBySeName(string blogCategorySeName)
        {
            if (string.IsNullOrEmpty(blogCategorySeName))
                throw new ArgumentNullException(nameof(blogCategorySeName));

            return await Task.FromResult(_blogCategoryRepository.Table.FirstOrDefault(x => x.SeName == blogCategorySeName.ToLowerInvariant()));
        }

        /// <summary>
        /// Get all blog categories
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IList<BlogCategory>> GetAllBlogCategories(string storeId = "")
        {
            var query = from c in _blogCategoryRepository.Table
                        select c;

            if (!string.IsNullOrEmpty(storeId) && !CommonHelper.IgnoreStoreLimitations)
            {
                query = query.Where(b => b.Stores.Contains(storeId) || !b.LimitedToStores);
            }

            return await Task.FromResult(query.OrderBy(x => x.DisplayOrder).ToList());
        }

        /// <summary>
        /// Inserts an blog category
        /// </summary>
        /// <param name="blogCategory">Blog category</param>
        public virtual async Task<BlogCategory> InsertBlogCategory(BlogCategory blogCategory)
        {
            if (blogCategory == null)
                throw new ArgumentNullException(nameof(blogCategory));

            await _blogCategoryRepository.InsertAsync(blogCategory);

            //event notification
            await _mediator.EntityInserted(blogCategory);

            return blogCategory;
        }

        /// <summary>
        /// Updates the blog category
        /// </summary>
        /// <param name="blogCategory">Blog category</param>
        public virtual async Task<BlogCategory> UpdateBlogCategory(BlogCategory blogCategory)
        {
            if (blogCategory == null)
                throw new ArgumentNullException(nameof(blogCategory));

            await _blogCategoryRepository.UpdateAsync(blogCategory);

            //event notification
            await _mediator.EntityUpdated(blogCategory);

            return blogCategory;
        }

        /// <summary>
        /// Delete blog category
        /// </summary>
        /// <param name="blogCategory">Blog category</param>
        public virtual async Task DeleteBlogCategory(BlogCategory blogCategory)
        {
            if (blogCategory == null)
                throw new ArgumentNullException(nameof(blogCategory));

            await _blogCategoryRepository.DeleteAsync(blogCategory);

            //event notification
            await _mediator.EntityDeleted(blogCategory);
        }

        #endregion

        #region Blog post product

        /// <summary>
        /// Gets a blog product
        /// </summary>
        /// <param name="id">id</param>
        /// <returns>Blog product</returns>
        public virtual Task<BlogProduct> GetBlogProductById(string id)
        {
            return _blogProductRepository.GetByIdAsync(id);
        }

        /// <summary>
        /// Insert an blog product
        /// </summary>
        /// <param name="blogProduct">Blog product</param>
        public virtual async Task InsertBlogProduct(BlogProduct blogProduct)
        {
            if (blogProduct == null)
                throw new ArgumentNullException(nameof(blogProduct));

            await _blogProductRepository.InsertAsync(blogProduct);

            //event notification
            await _mediator.EntityInserted(blogProduct);

        }

        /// <summary>
        /// Update an blog product
        /// </summary>
        /// <param name="blogProduct">Blog product</param>
        public virtual async Task UpdateBlogProduct(BlogProduct blogProduct)
        {
            if (blogProduct == null)
                throw new ArgumentNullException(nameof(blogProduct));

            await _blogProductRepository.UpdateAsync(blogProduct);

            //event notification
            await _mediator.EntityUpdated(blogProduct);

        }

        /// <summary>
        /// Delete an blog product
        /// </summary>
        /// <param name="blogProduct">Blog product</param>
        public virtual async Task DeleteBlogProduct(BlogProduct blogProduct)
        {
            if (blogProduct == null)
                throw new ArgumentNullException(nameof(blogProduct));

            await _blogProductRepository.DeleteAsync(blogProduct);

            //event notification
            await _mediator.EntityDeleted(blogProduct);

        }

        /// <summary>
        /// Get all product by blog post id
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IList<BlogProduct>> GetProductsByBlogPostId(string blogPostId)
        {
            if (string.IsNullOrEmpty(blogPostId))
                return new List<BlogProduct>();

            var query = from bp in _blogProductRepository.Table
                        where bp.BlogPostId == blogPostId
                        orderby bp.DisplayOrder
                        select bp;

            return await Task.FromResult(query.ToList());
        }

        #endregion


        #endregion
    }
}
