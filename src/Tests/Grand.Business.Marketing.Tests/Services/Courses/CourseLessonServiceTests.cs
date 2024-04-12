using Grand.Business.Marketing.Services.Courses;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Courses;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Marketing.Tests.Services.Courses;

[TestClass]
public class CourseLessonServiceTests
{
    private CourseLessonService _courseLessonService;
    private Mock<IMediator> _mediatorMock;
    private IRepository<CourseLesson> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<CourseLesson>();
        _mediatorMock = new Mock<IMediator>();
        _courseLessonService = new CourseLessonService(_repository, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task DeleteTest()
    {
        //Arrange
        var courseLesson = new CourseLesson {
            Name = "test"
        };
        await _courseLessonService.Insert(courseLesson);

        //Act
        await _courseLessonService.Delete(courseLesson);

        //Assert
        Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Name == "test"));
        Assert.AreEqual(0, _repository.Table.Count());
    }

    [TestMethod]
    public async Task GetByCourseIdTest()
    {
        //Arrange
        var courseLesson = new CourseLesson {
            Name = "test",
            CourseId = "1"
        };
        await _courseLessonService.Insert(courseLesson);

        //Act
        var result = await _courseLessonService.GetByCourseId(courseLesson.CourseId);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task GetByIdTest()
    {
        //Arrange
        var courseLesson = new CourseLesson {
            Name = "test"
        };
        await _courseLessonService.Insert(courseLesson);

        //Act
        var result = await _courseLessonService.GetById(courseLesson.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test", result.Name);
    }

    [TestMethod]
    public async Task InsertTest()
    {
        //Act
        var courseLesson = new CourseLesson {
            Name = "test"
        };
        await _courseLessonService.Insert(courseLesson);

        //Assert
        Assert.IsTrue(_repository.Table.Any());
    }

    [TestMethod]
    public async Task UpdateTest()
    {
        //Arrange
        var courseLesson = new CourseLesson {
            Name = "test"
        };
        await _courseLessonService.Insert(courseLesson);
        courseLesson.Name = "test2";

        //Act
        await _courseLessonService.Update(courseLesson);

        //Assert
        Assert.IsNotNull(_repository.Table.FirstOrDefault(x => x.Name == "test2"));
    }
}