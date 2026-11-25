using Grand.Business.Cms.Events;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Blogs;
using Grand.Domain.Seo;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Cms.Tests.Events;

[TestClass]
public class BlogPostDeletedEventHandlerTests
{
    private BlogPostDeletedEventHandler _handler;
    private Mock<ISlugService> _slugServiceMock;

    [TestInitialize]
    public void Init()
    {
        _slugServiceMock = new Mock<ISlugService>();
        _handler = new BlogPostDeletedEventHandler(_slugServiceMock.Object);
    }

    [TestMethod]
    public async Task Handle_InvokeSlugService()
    {
        _slugServiceMock.Setup(c => c.GetBySlug("post")).ReturnsAsync(new EntityUrl { Slug = "post" });
        await _handler.Handle(new EntityDeleted<BlogPost>(new BlogPost { SeName = "post" }), default);
        _slugServiceMock.Verify(c => c.DeleteEntityUrl(It.IsAny<EntityUrl>()), Times.Once);
    }
}