﻿using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Domain.News;
using Grand.Infrastructure;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Features.Models.News;
using Grand.Web.Models.Media;
using Grand.Web.Models.News;
using MediatR;

namespace Grand.Web.Features.Handlers.News
{
    public class GetNewsItemHandler : IRequestHandler<GetNewsItem, NewsItemModel>
    {
        private readonly IWorkContext _workContext;
        private readonly IDateTimeService _dateTimeService;
        private readonly IPictureService _pictureService;
        private readonly ITranslationService _translationService;
        private readonly ICustomerService _customerService;

        private readonly MediaSettings _mediaSettings;
        private readonly CaptchaSettings _captchaSettings;
        private readonly CustomerSettings _customerSettings;

        public GetNewsItemHandler(IWorkContext workContext, IDateTimeService dateTimeService,
            IPictureService pictureService, ITranslationService translationService, ICustomerService customerService,
            MediaSettings mediaSettings, CaptchaSettings captchaSettings, CustomerSettings customerSettings)
        {
            _workContext = workContext;
            _dateTimeService = dateTimeService;
            _pictureService = pictureService;
            _translationService = translationService;
            _customerService = customerService;

            _mediaSettings = mediaSettings;
            _captchaSettings = captchaSettings;
            _customerSettings = customerSettings;
        }

        public async Task<NewsItemModel> Handle(GetNewsItem request, CancellationToken cancellationToken)
        {
            var model = new NewsItemModel {
                Id = request.NewsItem.Id,
                MetaTitle = request.NewsItem.GetTranslation(x => x.MetaTitle, _workContext.WorkingLanguage.Id),
                MetaDescription = request.NewsItem.GetTranslation(x => x.MetaDescription, _workContext.WorkingLanguage.Id),
                MetaKeywords = request.NewsItem.GetTranslation(x => x.MetaKeywords, _workContext.WorkingLanguage.Id),
                SeName = request.NewsItem.GetSeName(_workContext.WorkingLanguage.Id),
                Title = request.NewsItem.GetTranslation(x => x.Title, _workContext.WorkingLanguage.Id),
                Short = request.NewsItem.GetTranslation(x => x.Short, _workContext.WorkingLanguage.Id),
                Full = request.NewsItem.GetTranslation(x => x.Full, _workContext.WorkingLanguage.Id),
                AllowComments = request.NewsItem.AllowComments,
                CreatedOn = _dateTimeService.ConvertToUserTime(request.NewsItem.StartDateUtc ?? request.NewsItem.CreatedOnUtc, DateTimeKind.Utc),
                NumberOfComments = request.NewsItem.CommentCount,
                AddNewComment = {
                    DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnNewsCommentPage
                }
            };

            //prepare comments
            await PrepareComments(request.NewsItem, model);

            //prepare picture model
            await PreparePicture(request.NewsItem, model);

            return model;
        }

        private async Task PrepareComments(NewsItem newsItem, NewsItemModel model)
        {
            var newsComments = newsItem.NewsComments.OrderBy(pr => pr.CreatedOnUtc);
            foreach (var nc in newsComments)
            {
                var customer = await _customerService.GetCustomerById(nc.CustomerId);
                var commentModel = new NewsCommentModel
                {
                    Id = nc.Id,
                    CustomerId = nc.CustomerId,
                    CustomerName = customer.FormatUserName(_customerSettings.CustomerNameFormat),
                    CommentTitle = nc.CommentTitle,
                    CommentText = nc.CommentText,
                    CreatedOn = _dateTimeService.ConvertToUserTime(nc.CreatedOnUtc, DateTimeKind.Utc)
                };
                model.Comments.Add(commentModel);
            }
        }

        private async Task PreparePicture(NewsItem newsItem, NewsItemModel model)
        {
            if (!string.IsNullOrEmpty(newsItem.PictureId))
            {
                model.PictureModel = new PictureModel
                {
                    Id = newsItem.PictureId,
                    FullSizeImageUrl = await _pictureService.GetPictureUrl(newsItem.PictureId),
                    ImageUrl = await _pictureService.GetPictureUrl(newsItem.PictureId, _mediaSettings.NewsThumbPictureSize),
                    Title = string.Format(_translationService.GetResource("Media.News.ImageLinkTitleFormat"), newsItem.Title),
                    AlternateText = string.Format(_translationService.GetResource("Media.News.ImageAlternateTextFormat"), newsItem.Title)
                };
            }
        }

    }
}
