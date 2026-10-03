using Grand.Data;
using Grand.Domain.Courses;
using Grand.Domain.Seo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Marketing.Events;

/// <summary>
///     Removes every slug of a deleted course, so its old link stops resolving and the name can be reused.
/// </summary>
public class CourseDeletedEventHandler : INotificationHandler<EntityDeleted<Course>>
{
    private readonly ICacheBase _cacheBase;
    private readonly IRepository<EntityUrl> _entityUrlRepository;

    public CourseDeletedEventHandler(IRepository<EntityUrl> entityUrlRepository, ICacheBase cacheBase)
    {
        _entityUrlRepository = entityUrlRepository;
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityDeleted<Course> notification, CancellationToken cancellationToken)
    {
        await _entityUrlRepository.DeleteManyAsync(x =>
            x.EntityId == notification.Entity.Id && x.EntityName == EntityTypes.Course);
        await _cacheBase.RemoveByPrefix(CacheKey.URLEntity_PATTERN_KEY);
    }
}
