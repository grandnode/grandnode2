using Grand.Business.Catalog.Services.Products;
using Grand.Business.Core.Events.Catalog;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Courses;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Products;

[TestClass]
public class ProductCourseServiceTests
{
    private IRepository<Course> _courseRepository;
    private ProductCourseService _courseService;
    private Mock<IMediator> _mediatorMock;
    private IRepository<Product> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Product>();
        _courseRepository = new MongoDBRepositoryTest<Course>();
        _mediatorMock = new Mock<IMediator>();
        _courseService = new ProductCourseService(_repository, _courseRepository, _mediatorMock.Object);
    }


    [TestMethod]
    public async Task GetCourseByProductIdTest()
    {
        //Arrange
        var course1 = new Course { ProductId = "1" };
        await _courseRepository.InsertAsync(course1);
        //Act
        var result = await _courseService.GetCourseByProductId("1");

        //Arrange
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetProductByCourseIdTest()
    {
        //Arrange
        var product = new Product { CourseId = "1", Id = "1" };
        await _repository.InsertAsync(product);
        //Act
        var result = await _courseService.GetProductByCourseId("1");

        //Arrange
        Assert.IsNotNull(result);
        Assert.AreEqual("1", result.Id);
    }

    [TestMethod]
    public async Task UpdateCourseOnProductTest()
    {
        //Arrange
        var product = new Product { CourseId = "1", Id = "1" };
        await _repository.InsertAsync(product);

        //Act
        await _courseService.UpdateCourseOnProduct("1", "2");

        //Arrange
        Assert.IsNotNull(_repository.Table.FirstOrDefault(x => x.Id == "1"));
        Assert.AreEqual("2", _repository.Table.FirstOrDefault(x => x.Id == "1").CourseId);
        _mediatorMock.Verify(m => m.Publish(It.Is<ProductChangedEvent>(e => e.ProductId == "1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}