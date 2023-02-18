﻿using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Blogs;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Web.Commands.Models.Blogs;
using MediatR;

namespace Grand.Web.Commands.Handler.Blogs
{
    public class InsertBlogCommentCommandHandler : IRequestHandler<InsertBlogCommentCommand, BlogComment>
    {
        private readonly IBlogService _blogService;
        private readonly IWorkContext _workContext;
        private readonly ICustomerService _customerService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IMessageProviderService _messageProviderService;
        private readonly ITranslationService _translationService;

        private readonly LanguageSettings _languageSettings;
        private readonly BlogSettings _blogSettings;

        public InsertBlogCommentCommandHandler(IBlogService blogService, IWorkContext workContext, 
            ICustomerService customerService, ICustomerActivityService customerActivityService, IMessageProviderService messageProviderService,
            ITranslationService translationService, LanguageSettings languageSettings, BlogSettings blogSettings)
        {
            _blogService = blogService;
            _workContext = workContext;
            _customerService = customerService;
            _customerActivityService = customerActivityService;
            _messageProviderService = messageProviderService;
            _translationService = translationService;

            _languageSettings = languageSettings;
            _blogSettings = blogSettings;
        }

        public async Task<BlogComment> Handle(InsertBlogCommentCommand request, CancellationToken cancellationToken)
        {
            var customer = _workContext.CurrentCustomer;
            var comment = new BlogComment
            {
                BlogPostId = request.BlogPost.Id,
                CustomerId = customer.Id,
                StoreId = _workContext.CurrentStore.Id,
                CommentText = request.Model.CommentText,
                CreatedOnUtc = DateTime.UtcNow,
                BlogPostTitle = request.BlogPost.Title
            };
            await _blogService.InsertBlogComment(comment);

            //update totals
            var comments = await _blogService.GetBlogCommentsByBlogPostId(request.BlogPost.Id);
            request.BlogPost.CommentCount = comments.Count;
            await _blogService.UpdateBlogPost(request.BlogPost);
            if (!customer.HasContributions)
            {
                await _customerService.UpdateContributions(customer);
            }
            //notify a store owner
            if (_blogSettings.NotifyAboutNewBlogComments)
                await _messageProviderService.SendBlogCommentMessage(request.BlogPost, comment, _languageSettings.DefaultAdminLanguageId);

            //activity log
            _ = _customerActivityService.InsertActivity("PublicStore.AddBlogComment", comment.Id, _workContext.CurrentCustomer, "", _translationService.GetResource("ActivityLog.PublicStore.AddBlogComment"));

            return comment;
        }
    }
}
