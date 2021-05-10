using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Storage.Interfaces;
using Grand.Infrastructure;
using Grand.Domain.Blogs;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Features.Models.Blogs;
using Grand.Web.Models.Blogs;
using Grand.Web.Models.Media;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Blogs
{
    public class GetBlogPostHandler : IRequestHandler<GetBlogPost, BlogPostModel>
    {
        private readonly IBlogService _blogService;
        private readonly IWorkContext _workContext;
        private readonly IDateTimeService _dateTimeService;
        private readonly IPictureService _pictureService;
        private readonly ITranslationService _translationService;
        private readonly ICustomerService _customerService;

        private readonly MediaSettings _mediaSettings;
        private readonly CaptchaSettings _captchaSettings;
        private readonly CustomerSettings _customerSettings;

        public GetBlogPostHandler(
           IBlogService blogService,
           IWorkContext workContext,
           IPictureService pictureService,
           ITranslationService translationService,
           IDateTimeService dateTimeService,
           ICustomerService customerService,
           CaptchaSettings captchaSettings,
           MediaSettings mediaSettings,
           CustomerSettings customerSettings)
        {
            _blogService = blogService;
            _workContext = workContext;
            _pictureService = pictureService;
            _translationService = translationService;
            _dateTimeService = dateTimeService;
            _customerService = customerService;

            _captchaSettings = captchaSettings;
            _mediaSettings = mediaSettings;
            _customerSettings = customerSettings;
        }

        public async Task<BlogPostModel> Handle(GetBlogPost request, CancellationToken cancellationToken)
        {
            if (request.BlogPost == null)
                throw new ArgumentNullException(nameof(request.BlogPost));

            var model = new BlogPostModel();

            model.Id = request.BlogPost.Id;
            model.MetaTitle = request.BlogPost.GetTranslation(x => x.MetaTitle, _workContext.WorkingLanguage.Id);
            model.MetaDescription = request.BlogPost.GetTranslation(x => x.MetaDescription, _workContext.WorkingLanguage.Id);
            model.MetaKeywords = request.BlogPost.GetTranslation(x => x.MetaKeywords, _workContext.WorkingLanguage.Id);
            model.SeName = request.BlogPost.GetSeName(_workContext.WorkingLanguage.Id);
            model.Title = request.BlogPost.GetTranslation(x => x.Title, _workContext.WorkingLanguage.Id);
            model.Body = request.BlogPost.GetTranslation(x => x.Body, _workContext.WorkingLanguage.Id);
            model.BodyOverview = request.BlogPost.GetTranslation(x => x.BodyOverview, _workContext.WorkingLanguage.Id);
            model.AllowComments = request.BlogPost.AllowComments;
            model.CreatedOn = _dateTimeService.ConvertToUserTime(request.BlogPost.StartDateUtc ?? request.BlogPost.CreatedOnUtc, DateTimeKind.Utc);
            model.Tags = request.BlogPost.ParseTags().ToList();
            model.NumberOfComments = request.BlogPost.CommentCount;
            model.AddNewComment.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnBlogCommentPage;
            model.UserFields = request.BlogPost.UserFields;

            var blogComments = await _blogService.GetBlogCommentsByBlogPostId(request.BlogPost.Id);
            foreach (var bc in blogComments)
            {
                var commentModel = await PrepareBlogPostCommentModel(bc);
                model.Comments.Add(commentModel);
            }

            //prepare picture model
            await PrepareBlogPostPictureModel(model, request.BlogPost);

            return model;
        }
        private async Task PrepareBlogPostPictureModel(BlogPostModel model, BlogPost blogPost)
        {
            if (!string.IsNullOrEmpty(blogPost.PictureId))
            {
                var pictureModel = new PictureModel
                {
                    Id = blogPost.PictureId,
                    FullSizeImageUrl = await _pictureService.GetPictureUrl(blogPost.PictureId),
                    ImageUrl = await _pictureService.GetPictureUrl(blogPost.PictureId, _mediaSettings.BlogThumbPictureSize),
                    Title = string.Format(_translationService.GetResource("Media.Blog.ImageLinkTitleFormat"), blogPost.Title),
                    AlternateText = string.Format(_translationService.GetResource("Media.Blog.ImageAlternateTextFormat"), blogPost.Title)
                };

                model.PictureModel = pictureModel;
            }
        }

        private async Task<BlogCommentModel> PrepareBlogPostCommentModel(BlogComment blogComment)
        {
            var customer = await _customerService.GetCustomerById(blogComment.CustomerId);
            var model = new BlogCommentModel
            {
                Id = blogComment.Id,
                CustomerId = blogComment.CustomerId,
                CustomerName = customer.FormatUserName(_customerSettings.CustomerNameFormat),
                CommentText = blogComment.CommentText,
                CreatedOn = _dateTimeService.ConvertToUserTime(blogComment.CreatedOnUtc, DateTimeKind.Utc),
            };
            return model;
        }
    }
}
