using Grand.Business.Marketing.Events;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Courses;
using Grand.Domain.Seo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Events;
using Grand.Infrastructure.Tests.Caching;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Marketing.Tests.Events;

[TestClass]
public class CourseDeletedEventHandlerTests
{
    [TestMethod]
    public async Task Handle_DeletesEveryUrlOfTheCourse_KeepsOthers()
    {
        var entityUrlRepository = new MongoDBRepositoryTest<EntityUrl>();
        var cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), new Mock<IMediator>().Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        var handler = new CourseDeletedEventHandler(entityUrlRepository, cacheBase);
        var course = new Course();
        await entityUrlRepository.InsertAsync(new EntityUrl { EntityId = course.Id, EntityName = EntityTypes.Course, Slug = "course-en" });
        await entityUrlRepository.InsertAsync(new EntityUrl { EntityId = course.Id, EntityName = EntityTypes.Course, Slug = "course-pl", LanguageId = "pl" });
        await entityUrlRepository.InsertAsync(new EntityUrl { EntityId = "other", EntityName = EntityTypes.Course, Slug = "other-course" });

        await handler.Handle(new EntityDeleted<Course>(course), CancellationToken.None);

        Assert.AreEqual(0, entityUrlRepository.Table.Count(x => x.EntityId == course.Id));
        Assert.AreEqual(1, entityUrlRepository.Table.Count());
    }
}
