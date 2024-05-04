using Grand.Business.Catalog.Services.Products;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Products;

[TestClass]
public class CustomerGroupProductServiceTests
{
    private Mock<ICacheBase> _cacheMock;
    private CustomerGroupProductService _custometGroupService;
    private Mock<IMediator> _mediatorMock;
    private Mock<IRepository<CustomerGroupProduct>> _repositoryMock;

    [TestInitialize]
    public void Init()
    {
        _repositoryMock = new Mock<IRepository<CustomerGroupProduct>>();
        _mediatorMock = new Mock<IMediator>();
        _cacheMock = new Mock<ICacheBase>();
        _custometGroupService =
            new CustomerGroupProductService(_repositoryMock.Object, _cacheMock.Object, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task DeleteCustomerGroupProduct_ValidArguments_InvokeMethods()
    {
        await _custometGroupService.DeleteCustomerGroupProduct(new CustomerGroupProduct());
        _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<CustomerGroupProduct>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true), Times.Exactly(1));
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<CustomerGroupProduct>>(), default), Times.Once);
    }


    [TestMethod]
    public async Task InsertCustomerGroupProduct_ValidArguments_InvokeMethods()
    {
        await _custometGroupService.InsertCustomerGroupProduct(new CustomerGroupProduct());
        _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<CustomerGroupProduct>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true), Times.Exactly(1));
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<CustomerGroupProduct>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task GetCustomerGroupProducts_ValidArguments_InvokeMethods()
    {
        await _custometGroupService.GetCustomerGroupProducts("id");
        //get by cache 
        _cacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<CustomerGroupProduct>>>>()),
            Times.Exactly(1));
    }
}