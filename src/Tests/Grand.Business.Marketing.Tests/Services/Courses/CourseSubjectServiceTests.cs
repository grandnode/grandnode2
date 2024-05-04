using Grand.Business.Marketing.Services.Courses;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Courses;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Marketing.Tests.Services.Courses;

[TestClass]
public class CourseSubjectServiceTests
{
    private CourseSubjectService _courseSubjectService;
    private Mock<IMediator> _mediatorMock;
    private IRepository<CourseSubject> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<CourseSubject>();
        _mediatorMock = new Mock<IMediator>();
        _courseSubjectService = new CourseSubjectService(_repository, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task DeleteTest()
    {
        //Arrange
        var courseSubject = new CourseSubject {
            Name = "test"
        };
        await _courseSubjectService.Insert(courseSubject);

        //Act
        await _courseSubjectService.Delete(courseSubject);

        //Assert
        Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Name == "test"));
        Assert.AreEqual(0, _repository.Table.Count());
    }

    [TestMethod]
    public async Task GetByCourseIdTest()
    {
        //Arrange
        var courseSubject = new CourseSubject {
            Name = "test",
            CourseId = "1"
        };
        await _courseSubjectService.Insert(courseSubject);

        //Act
        var result = await _courseSubjectService.GetByCourseId(courseSubject.CourseId);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task GetByIdTest()
    {
        //Arrange
        var courseSubject = new CourseSubject {
            Name = "test"
        };
        await _courseSubjectService.Insert(courseSubject);

        //Act
        var result = await _courseSubjectService.GetById(courseSubject.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test", result.Name);
    }

    [TestMethod]
    public async Task InsertTest()
    {
        //Act
        var courseSubject = new CourseSubject {
            Name = "test"
        };
        await _courseSubjectService.Insert(courseSubject);

        //Assert
        Assert.IsTrue(_repository.Table.Any());
    }

    [TestMethod]
    public async Task UpdateTest()
    {
        //Arrange
        var courseSubject = new CourseSubject {
            Name = "test"
        };
        await _courseSubjectService.Insert(courseSubject);
        courseSubject.Name = "test2";

        //Act
        await _courseSubjectService.Update(courseSubject);

        //Assert
        Assert.IsNotNull(_repository.Table.FirstOrDefault(x => x.Name == "test2"));
    }
}