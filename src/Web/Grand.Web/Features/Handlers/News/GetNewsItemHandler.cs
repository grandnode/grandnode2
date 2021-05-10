using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Storage.Interfaces;
using Grand.Web.Common.Security.Captcha;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Domain.News;
using Grand.Infrastructure;
using Grand.Web.Features.Models.News;
using Grand.Web.Models.Media;
using Grand.Web.Models.News;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            var model = new NewsItemModel();
            model.Id = request.NewsItem.Id;
            model.MetaTitle = request.NewsItem.GetTranslation(x => x.MetaTitle, _workContext.WorkingLanguage.Id);
            model.MetaDescription = request.NewsItem.GetTranslation(x => x.MetaDescription, _workContext.WorkingLanguage.Id);
            model.MetaKeywords = request.NewsItem.GetTranslation(x => x.MetaKeywords, _workContext.WorkingLanguage.Id);
            model.SeName = request.NewsItem.GetSeName(_workContext.WorkingLanguage.Id);
            model.Title = request.NewsItem.GetTranslation(x => x.Title, _workContext.WorkingLanguage.Id);
            model.Short = request.NewsItem.GetTranslation(x => x.Short, _workContext.WorkingLanguage.Id);
            model.Full = request.NewsItem.GetTranslation(x => x.Full, _workContext.WorkingLanguage.Id);
            model.AllowComments = request.NewsItem.AllowComments;
            model.CreatedOn = _dateTimeService.ConvertToUserTime(request.NewsItem.StartDateUtc ?? request.NewsItem.CreatedOnUtc, DateTimeKind.Utc);
            model.NumberOfComments = request.NewsItem.CommentCount;
            model.AddNewComment.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnNewsCommentPage;

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
                    CreatedOn = _dateTimeService.ConvertToUserTime(nc.CreatedOnUtc, DateTimeKind.Utc),
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
