using Grand.Business.Customers.Services;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Tests.Services;

[TestClass]
public class CustomerAttributeServiceTests
{
    private CustomerAttributeService _atrService;
    private Mock<ICacheBase> _cacheMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IRepository<CustomerAttribute>> _repositoryMock;

    [TestInitialize]
    public void Init()
    {
        _cacheMock = new Mock<ICacheBase>();
        _repositoryMock = new Mock<IRepository<CustomerAttribute>>();
        _mediatorMock = new Mock<IMediator>();
        _atrService = new CustomerAttributeService(_cacheMock.Object, _repositoryMock.Object, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task InsertCustomerAttribute_ValidArguemnts_InvokeRepositoryAndCache()
    {
        await _atrService.InsertCustomerAttribute(new CustomerAttribute());
        _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<CustomerAttribute>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<CustomerAttribute>>(), default), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true));
    }

    [TestMethod]
    public async Task UpdateCustomerAttribute_ValidArguemnts_InvokeRepositoryAndCache()
    {
        await _atrService.UpdateCustomerAttribute(new CustomerAttribute());
        _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<CustomerAttribute>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<CustomerAttribute>>(), default), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true));
    }

    [TestMethod]
    public async Task DeleteCustomerAttribute_ValidArguemnts_InvokeRepositoryAndCache()
    {
        await _atrService.DeleteCustomerAttribute(new CustomerAttribute());
        _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<CustomerAttribute>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<CustomerAttribute>>(), default), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true));
    }

    [TestMethod]
    public async Task InsertCustomerAttributeValue_ValidArguemnts_InvokeRepositoryAndCache()
    {
        _repositoryMock.Setup(c => c.GetByIdAsync(It.IsAny<string>()))
            .Returns(() => Task.FromResult(new CustomerAttribute()));
        await _atrService.InsertCustomerAttributeValue(new CustomerAttributeValue());
        _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<CustomerAttribute>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<CustomerAttributeValue>>(), default), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true));
    }

    [TestMethod]
    public async Task UpdateCustomerAttributeValue_ValidArguemnts_InvokeRepositoryAndCache()
    {
        _repositoryMock.Setup(c => c.GetByIdAsync(It.IsAny<string>()))
            .Returns(() => Task.FromResult(new CustomerAttribute()));
        await _atrService.UpdateCustomerAttributeValue(new CustomerAttributeValue());
        _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<CustomerAttribute>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<CustomerAttributeValue>>(), default), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true));
    }

    [TestMethod]
    public async Task DeleteCustomerAttributeValue_ValidArguemnts_InvokeRepositoryAndCache()
    {
        _repositoryMock.Setup(c => c.GetByIdAsync(It.IsAny<string>()))
            .Returns(() => Task.FromResult(new CustomerAttribute()));
        await _atrService.DeleteCustomerAttributeValue(new CustomerAttributeValue());
        _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<CustomerAttribute>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<CustomerAttributeValue>>(), default), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true));
    }

    [TestMethod]
    public async Task GetAllCustomerAttributes_InvokeRepositoryAndCache()
    {
        await _atrService.GetAllCustomerAttributes();
        _cacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<CustomerAttribute>>>>()),
            Times.Once);
    }
}