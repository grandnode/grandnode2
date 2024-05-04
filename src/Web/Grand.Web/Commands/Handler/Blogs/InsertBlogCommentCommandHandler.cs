using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Blogs;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Web.Commands.Models.Blogs;
using MediatR;

namespace Grand.Web.Commands.Handler.Blogs;

public class InsertBlogCommentCommandHandler : IRequestHandler<InsertBlogCommentCommand, BlogComment>
{
    private readonly IBlogService _blogService;
    private readonly BlogSettings _blogSettings;
    private readonly ICustomerService _customerService;

    private readonly LanguageSettings _languageSettings;
    private readonly IMessageProviderService _messageProviderService;
    private readonly IWorkContext _workContext;

    public InsertBlogCommentCommandHandler(IBlogService blogService, IWorkContext workContext,
        ICustomerService customerService, IMessageProviderService messageProviderService,
        LanguageSettings languageSettings, BlogSettings blogSettings)
    {
        _blogService = blogService;
        _workContext = workContext;
        _customerService = customerService;
        _messageProviderService = messageProviderService;

        _languageSettings = languageSettings;
        _blogSettings = blogSettings;
    }

    public async Task<BlogComment> Handle(InsertBlogCommentCommand request, CancellationToken cancellationToken)
    {
        var customer = _workContext.CurrentCustomer;
        var comment = new BlogComment {
            BlogPostId = request.BlogPost.Id,
            CustomerId = customer.Id,
            StoreId = _workContext.CurrentStore.Id,
            CommentText = request.Model.CommentText,
            BlogPostTitle = request.BlogPost.Title
        };
        await _blogService.InsertBlogComment(comment);

        //update totals
        var comments = await _blogService.GetBlogCommentsByBlogPostId(request.BlogPost.Id);
        request.BlogPost.CommentCount = comments.Count;
        await _blogService.UpdateBlogPost(request.BlogPost);
        if (!customer.HasContributions) await _customerService.UpdateContributions(customer);
        //notify a store owner
        if (_blogSettings.NotifyAboutNewBlogComments)
            await _messageProviderService.SendBlogCommentMessage(request.BlogPost, comment,
                _languageSettings.DefaultAdminLanguageId);

        return comment;
    }
}