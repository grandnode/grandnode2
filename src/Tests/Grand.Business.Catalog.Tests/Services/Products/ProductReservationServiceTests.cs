using Grand.Business.Catalog.Services.Products;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Products;

[TestClass]
public class ProductReservationServiceTests
{
    private Mock<IMediator> _mediatorMock;
    private IRepository<ProductReservation> _repository;
    private IRepository<CustomerReservationsHelper> _repositoryCustomerReservationsHelper;

    private ProductReservationService _service;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<ProductReservation>();
        _repositoryCustomerReservationsHelper = new MongoDBRepositoryTest<CustomerReservationsHelper>();
        _mediatorMock = new Mock<IMediator>();

        _service = new ProductReservationService(_repository, _repositoryCustomerReservationsHelper,
            _mediatorMock.Object);
    }

    [TestMethod]
    public async Task GetProductReservationsByProductIdTest()
    {
        //Arrange
        await _repository.InsertAsync(new ProductReservation { ProductId = "1" });
        await _repository.InsertAsync(new ProductReservation { ProductId = "1" });
        await _repository.InsertAsync(new ProductReservation { ProductId = "2" });
        //Act
        var result = await _service.GetProductReservationsByProductId("1", null, null);

        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public async Task InsertProductReservationTest()
    {
        //Arrange
        var p = new ProductReservation { ProductId = "1" };
        //Act
        await _service.InsertProductReservation(p);
        //Assert
        Assert.AreEqual(1, _repository.Table.Count());
    }

    [TestMethod]
    public async Task UpdateProductReservationTest()
    {
        //Arrange
        var p = new ProductReservation { ProductId = "1" };
        await _repository.InsertAsync(p);
        //Act
        p.ProductId = "2";
        await _service.UpdateProductReservation(p);

        Assert.AreEqual("2", _repository.Table.FirstOrDefault().ProductId);
    }

    [TestMethod]
    public async Task DeleteProductReservationTest()
    {
        //Arrange
        var p = new ProductReservation { ProductId = "1" };
        await _repository.InsertAsync(p);
        //Act
        await _service.DeleteProductReservation(p);
        //Assert
        Assert.AreEqual(0, _repository.Table.Count());
    }

    [TestMethod]
    public async Task GetProductReservationTest()
    {
        //Arrange
        var p = new ProductReservation { ProductId = "1" };
        await _repository.InsertAsync(p);
        //Act
        var result = await _service.GetProductReservation(p.Id);
        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("1", result.ProductId);
    }

    [TestMethod]
    public async Task InsertCustomerReservationsHelperTest()
    {
        //Arrange
        var p = new CustomerReservationsHelper();
        //Act
        await _service.InsertCustomerReservationsHelper(p);
        //Assert
        Assert.AreEqual(1, _repositoryCustomerReservationsHelper.Table.Count());
    }

    [TestMethod]
    public async Task DeleteCustomerReservationsHelperTest()
    {
        //Arrange
        var p = new CustomerReservationsHelper();
        await _repositoryCustomerReservationsHelper.InsertAsync(p);
        //Act
        await _service.DeleteCustomerReservationsHelper(p);
        //Assert
        Assert.AreEqual(0, _repository.Table.Count());
    }

    [TestMethod]
    public async Task CancelReservationsByOrderIdTest()
    {
        //Arrange
        await _repository.InsertAsync(new ProductReservation { OrderId = "1" });
        await _repository.InsertAsync(new ProductReservation { OrderId = "2" });
        //Act
        await _service.CancelReservationsByOrderId("1");
        //Assert
        Assert.AreEqual(0, _repository.Table.Where(x => x.OrderId == "1").Count());
    }

    [TestMethod]
    public async Task GetCustomerReservationsHelperByIdTest()
    {
        //Arrange
        var p = new CustomerReservationsHelper { CustomerId = "1" };
        await _repositoryCustomerReservationsHelper.InsertAsync(p);
        //Act
        var result = await _service.GetCustomerReservationsHelperById(p.Id);
        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("1", result.CustomerId);
    }

    [TestMethod]
    public async Task GetCustomerReservationsHelpersTest()
    {
        //Arrange
        await _repositoryCustomerReservationsHelper.InsertAsync(new CustomerReservationsHelper { CustomerId = "1" });
        await _repositoryCustomerReservationsHelper.InsertAsync(new CustomerReservationsHelper { CustomerId = "1" });
        await _repositoryCustomerReservationsHelper.InsertAsync(new CustomerReservationsHelper { CustomerId = "2" });
        //Act
        var result = await _service.GetCustomerReservationsHelpers("1");
        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public async Task GetCustomerReservationsHelperBySciIdTest()
    {
        //Arrange
        await _repositoryCustomerReservationsHelper.InsertAsync(new CustomerReservationsHelper
            { ShoppingCartItemId = "1" });
        await _repositoryCustomerReservationsHelper.InsertAsync(new CustomerReservationsHelper
            { ShoppingCartItemId = "1" });
        await _repositoryCustomerReservationsHelper.InsertAsync(new CustomerReservationsHelper { CustomerId = "2" });
        //Act
        var result = await _service.GetCustomerReservationsHelperBySciId("1");
        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count);
    }
}