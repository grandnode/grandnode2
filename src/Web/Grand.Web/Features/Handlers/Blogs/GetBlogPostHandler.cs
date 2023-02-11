﻿using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Blogs;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Infrastructure;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Features.Models.Blogs;
using Grand.Web.Models.Blogs;
using Grand.Web.Models.Media;
using MediatR;

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

            var model = new BlogPostModel {
                Id = request.BlogPost.Id,
                MetaTitle = request.BlogPost.GetTranslation(x => x.MetaTitle, _workContext.WorkingLanguage.Id),
                MetaDescription = request.BlogPost.GetTranslation(x => x.MetaDescription, _workContext.WorkingLanguage.Id),
                MetaKeywords = request.BlogPost.GetTranslation(x => x.MetaKeywords, _workContext.WorkingLanguage.Id),
                SeName = request.BlogPost.GetSeName(_workContext.WorkingLanguage.Id),
                Title = request.BlogPost.GetTranslation(x => x.Title, _workContext.WorkingLanguage.Id),
                Body = request.BlogPost.GetTranslation(x => x.Body, _workContext.WorkingLanguage.Id),
                BodyOverview = request.BlogPost.GetTranslation(x => x.BodyOverview, _workContext.WorkingLanguage.Id),
                AllowComments = request.BlogPost.AllowComments,
                CreatedOn = _dateTimeService.ConvertToUserTime(request.BlogPost.StartDateUtc ?? request.BlogPost.CreatedOnUtc, DateTimeKind.Utc),
                Tags = request.BlogPost.ParseTags().ToList(),
                NumberOfComments = request.BlogPost.CommentCount,
                AddNewComment = {
                    DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnBlogCommentPage
                },
                UserFields = request.BlogPost.UserFields
            };

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
                var picture = await _pictureService.GetPictureById(blogPost.PictureId);
                var pictureModel = new PictureModel
                {
                    Id = blogPost.PictureId,
                    FullSizeImageUrl = await _pictureService.GetPictureUrl(blogPost.PictureId),
                    ImageUrl = await _pictureService.GetPictureUrl(blogPost.PictureId, _mediaSettings.BlogThumbPictureSize),
                    Style = picture?.Style,
                    ExtraField = picture?.ExtraField,
                    //"title" attribute
                    Title = picture != null && !string.IsNullOrEmpty(picture.GetTranslation(x => x.TitleAttribute, _workContext.WorkingLanguage.Id)) ?
                        picture.GetTranslation(x => x.TitleAttribute, _workContext.WorkingLanguage.Id) :
                        string.Format(_translationService.GetResource("Media.Blog.ImageLinkTitleFormat"), blogPost.Title),
                    //"alt" attribute
                    AlternateText = picture != null && !string.IsNullOrEmpty(picture.GetTranslation(x => x.AltAttribute, _workContext.WorkingLanguage.Id)) ?
                        picture.GetTranslation(x => x.AltAttribute, _workContext.WorkingLanguage.Id) :
                        string.Format(_translationService.GetResource("Media.Blog.ImageAlternateTextFormat"), blogPost.Title)
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
                CreatedOn = _dateTimeService.ConvertToUserTime(blogComment.CreatedOnUtc, DateTimeKind.Utc)
            };
            return model;
        }
    }
}
