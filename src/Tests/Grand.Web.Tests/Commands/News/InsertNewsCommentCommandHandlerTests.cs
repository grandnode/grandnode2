using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.News;
using Grand.Infrastructure;
using Grand.Web.Commands.Handler.News;
using Grand.Web.Commands.Models.News;
using Grand.Web.Models.News;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Commands.News;

[TestClass]
public class InsertNewsCommentCommandHandlerTests
{
    [TestMethod]
    public async Task Handle_StampsTheCommentWithTheTimeItWasPosted()
    {
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(new Customer { Id = "c1" });
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(x => x.CurrentStore).Returns(new Grand.Domain.Stores.Store { Id = "s1" });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);
        contextAccessor.Setup(x => x.StoreContext).Returns(storeContext.Object);
        var handler = new InsertNewsCommentCommandHandler(contextAccessor.Object, new Mock<INewsService>().Object,
            new Mock<ICustomerService>().Object, new Mock<ICmsMessageProviderService>().Object, new NewsSettings(),
            new LanguageSettings());
        var before = DateTime.UtcNow;

        var comment = await handler.Handle(new InsertNewsCommentCommand {
            NewsItem = new NewsItem { Id = "n1" },
            Model = new AddNewsCommentModel { CommentTitle = "title", CommentText = "text" }
        }, CancellationToken.None);

        Assert.IsTrue(comment.CreatedOnUtc >= before && comment.CreatedOnUtc <= DateTime.UtcNow);
    }
}
