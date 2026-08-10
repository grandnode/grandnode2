using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Messages.Services;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Events;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Services;

[TestClass]
public class MessageTemplateServiceTests
{
    private Mock<IAclService> _aclService;
    private Mock<ICacheBase> _cacheMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IRepository<MessageTemplate>> _repositoryMock;
    private MessageTemplateService _service;
    private CatalogSettings _settings;

    [TestInitialize]
    public void Init()
    {
        _cacheMock = new Mock<ICacheBase>();
        _aclService = new Mock<IAclService>();
        _repositoryMock = new Mock<IRepository<MessageTemplate>>();
        _mediatorMock = new Mock<IMediator>();
        _settings = new CatalogSettings();
        var accessControlConfig = new AccessControlConfig();
        _service = new MessageTemplateService(_cacheMock.Object, _aclService.Object, _repositoryMock.Object,
            _mediatorMock.Object, accessControlConfig);
    }

    [TestMethod]
    public void CopyMessageTemplate_NullArrguemnt_ThrowException()
    {
        Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await _service.CopyMessageTemplate(null));
    }

    [TestMethod]
    public async Task CopyMessageTemplate_InsertCopyEntity()
    {
        var template = new MessageTemplate {
            Id = "id1",
            Name = "Name"
        };

        var result = await _service.CopyMessageTemplate(template);
        Assert.AreEqual(template.Name, result.Name);
        Assert.AreNotEqual(template.Id, result.Id);
        //should be insert into db
        _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<MessageTemplate>()), Times.Once);
    }

    /// <summary>
    ///     The store panel overrides a shared template by copying it for one store, which leaves two
    ///     templates carrying one name visible to that store. The store's own copy is inserted second,
    ///     so ordering by identifier alone sent the store's mail from the shared template.
    /// </summary>
    [TestMethod]
    public async Task GetMessageTemplateByName_StoreHasItsOwnCopy_ReturnsTheCopyNotTheSharedTemplate()
    {
        var sharedTemplate = new MessageTemplate { Id = "id1", Name = "Name", LimitedToStores = false };
        var storeCopy = new MessageTemplate { Id = "id2", Name = "Name", LimitedToStores = true, Stores = { "store-1" } };
        ArrangeLookup(sharedTemplate, storeCopy);

        var result = await _service.GetMessageTemplateByName("Name", "store-1");

        Assert.AreEqual(storeCopy.Id, result.Id);
    }

    /// <summary>
    ///     A store without a copy of its own keeps resolving to the shared template.
    /// </summary>
    [TestMethod]
    public async Task GetMessageTemplateByName_AnotherStoreHasTheCopy_ReturnsTheSharedTemplate()
    {
        var sharedTemplate = new MessageTemplate { Id = "id1", Name = "Name", LimitedToStores = false };
        var storeCopy = new MessageTemplate { Id = "id2", Name = "Name", LimitedToStores = true, Stores = { "store-1" } };
        ArrangeLookup(sharedTemplate, storeCopy);

        var result = await _service.GetMessageTemplateByName("Name", "store-2");

        Assert.AreEqual(sharedTemplate.Id, result.Id);
    }

    private void ArrangeLookup(params MessageTemplate[] templates)
    {
        _repositoryMock.Setup(c => c.Table).Returns(templates.AsQueryable());
        _repositoryMock
            .Setup(c => c.ToListAsync(It.IsAny<IQueryable<MessageTemplate>>(), It.IsAny<CancellationToken>()))
            .Returns((IQueryable<MessageTemplate> query, CancellationToken _) =>
                Task.FromResult<IList<MessageTemplate>>(query.ToList()));
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<MessageTemplate>>>()))
            .Returns((string _, Func<Task<MessageTemplate>> acquire) => acquire());
        _aclService.Setup(c => c.Authorize(It.IsAny<MessageTemplate>(), It.IsAny<string>()))
            .Returns((MessageTemplate t, string storeId) => !t.LimitedToStores || t.Stores.Contains(storeId));
    }

    [TestMethod]
    public async Task InsertMessageTemplate_InvokeExpectedMethods()
    {
        await _service.InsertMessageTemplate(new MessageTemplate());
        _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<MessageTemplate>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<MessageTemplate>>(), default), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public async Task UpdateMessageTemplate_InvokeExpectedMethods()
    {
        await _service.UpdateMessageTemplate(new MessageTemplate());
        _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<MessageTemplate>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<MessageTemplate>>(), default), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public async Task DeleteMessageTemplate_InvokeExpectedMethods()
    {
        await _service.DeleteMessageTemplate(new MessageTemplate());
        _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<MessageTemplate>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<MessageTemplate>>(), default), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
    }
}