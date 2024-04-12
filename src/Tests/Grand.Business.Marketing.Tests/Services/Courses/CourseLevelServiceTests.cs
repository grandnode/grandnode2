using Grand.Business.Marketing.Services.Courses;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Courses;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Marketing.Tests.Services.Courses;

[TestClass]
public class CourseLevelServiceTests
{
    private CourseLevelService _courseLevelService;
    private Mock<IMediator> _mediatorMock;
    private IRepository<CourseLevel> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<CourseLevel>();
        _mediatorMock = new Mock<IMediator>();
        _courseLevelService = new CourseLevelService(_repository, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task DeleteTest()
    {
        //Arrange
        var courseLevel = new CourseLevel {
            Name = "test"
        };
        await _courseLevelService.Insert(courseLevel);

        //Act
        await _courseLevelService.Delete(courseLevel);

        //Assert
        Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Name == "test"));
        Assert.AreEqual(0, _repository.Table.Count());
    }

    [TestMethod]
    public async Task GetAllTest()
    {
        //Arrange
        await _courseLevelService.Insert(new CourseLevel());
        await _courseLevelService.Insert(new CourseLevel());
        await _courseLevelService.Insert(new CourseLevel());

        //Act
        var result = await _courseLevelService.GetAll();

        //Assert
        Assert.AreEqual(3, result.Count);
    }

    [TestMethod]
    public async Task GetByIdTest()
    {
        //Arrange
        var courseLevel = new CourseLevel {
            Name = "test"
        };
        await _courseLevelService.Insert(courseLevel);

        //Act
        var result = await _courseLevelService.GetById(courseLevel.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test", result.Name);
    }

    [TestMethod]
    public async Task InsertTest()
    {
        //Act
        var courseLevel = new CourseLevel {
            Name = "test"
        };
        await _courseLevelService.Insert(courseLevel);

        //Assert
        Assert.IsTrue(_repository.Table.Any());
    }

    [TestMethod]
    public async Task UpdateTest()
    {
        //Arrange
        var courseLevel = new CourseLevel {
            Name = "test"
        };
        await _courseLevelService.Insert(courseLevel);
        courseLevel.Name = "test2";

        //Act
        await _courseLevelService.Update(courseLevel);

        //Assert
        Assert.IsNotNull(_repository.Table.FirstOrDefault(x => x.Name == "test2"));
    }
}