using Grand.Business.Cms.Interfaces;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Messages.Interfaces;
using Grand.Infrastructure;
using Grand.Domain.Localization;
using Grand.Domain.News;
using Grand.Web.Commands.Models.News;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Commands.Handler.News
{
    public class InsertNewsCommentCommandHandler : IRequestHandler<InsertNewsCommentCommand, NewsComment>
    {
        private readonly IWorkContext _workContext;
        private readonly INewsService _newsService;
        private readonly ICustomerService _customerService;
        private readonly IMessageProviderService _messageProviderService;

        private readonly NewsSettings _newsSettings;
        private readonly LanguageSettings _languageSettings;

        public InsertNewsCommentCommandHandler(IWorkContext workContext, INewsService newsService,
            ICustomerService customerService, IMessageProviderService messageProviderService, NewsSettings newsSettings,
            LanguageSettings languageSettings)
        {
            _workContext = workContext;
            _newsService = newsService;
            _customerService = customerService;
            _messageProviderService = messageProviderService;

            _newsSettings = newsSettings;
            _languageSettings = languageSettings;
        }

        public async Task<NewsComment> Handle(InsertNewsCommentCommand request, CancellationToken cancellationToken)
        {
            var comment = new NewsComment
            {
                NewsItemId = request.NewsItem.Id,
                CustomerId = _workContext.CurrentCustomer.Id,
                StoreId = _workContext.CurrentStore.Id,
                CommentTitle = request.Model.AddNewComment.CommentTitle,
                CommentText = request.Model.AddNewComment.CommentText,
                CreatedOnUtc = DateTime.UtcNow,
            };
            request.NewsItem.NewsComments.Add(comment);

            //update totals
            request.NewsItem.CommentCount = request.NewsItem.NewsComments.Count;

            await _newsService.UpdateNews(request.NewsItem);

            await _customerService.UpdateContributions(_workContext.CurrentCustomer);

            //notify a store owner;
            if (_newsSettings.NotifyAboutNewNewsComments)
                await _messageProviderService.SendNewsCommentMessage(request.NewsItem, comment, _languageSettings.DefaultAdminLanguageId);

            return comment;
        }
    }
}
