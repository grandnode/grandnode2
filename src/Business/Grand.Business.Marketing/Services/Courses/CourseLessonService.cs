using Grand.Business.Core.Interfaces.Marketing.Courses;
using Grand.Data;
using Grand.Domain.Courses;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Marketing.Services.Courses;

public class CourseLessonService : ICourseLessonService
{
    private readonly IRepository<CourseLesson> _courseLessonRepository;
    private readonly IMediator _mediator;

    public CourseLessonService(IRepository<CourseLesson> courseLessonRepository, IMediator mediator)
    {
        _courseLessonRepository = courseLessonRepository;
        _mediator = mediator;
    }

    public virtual async Task Delete(CourseLesson courseLesson)
    {
        ArgumentNullException.ThrowIfNull(courseLesson);

        await _courseLessonRepository.DeleteAsync(courseLesson);

        //event notification
        await _mediator.EntityDeleted(courseLesson);
    }

    public virtual async Task<IList<CourseLesson>> GetByCourseId(string courseId)
    {
        if (string.IsNullOrEmpty(courseId))
            throw new ArgumentNullException(nameof(courseId));

        var query = from c in _courseLessonRepository.Table
            where c.CourseId == courseId
            select c;

        return await Task.FromResult(query.ToList());
    }

    public virtual Task<CourseLesson> GetById(string id)
    {
        return _courseLessonRepository.GetByIdAsync(id);
    }

    public virtual async Task<CourseLesson> Insert(CourseLesson courseLesson)
    {
        ArgumentNullException.ThrowIfNull(courseLesson);

        await _courseLessonRepository.InsertAsync(courseLesson);

        //event notification
        await _mediator.EntityInserted(courseLesson);

        return courseLesson;
    }

    public virtual async Task<CourseLesson> Update(CourseLesson courseLesson)
    {
        ArgumentNullException.ThrowIfNull(courseLesson);

        await _courseLessonRepository.UpdateAsync(courseLesson);

        //event notification
        await _mediator.EntityUpdated(courseLesson);

        return courseLesson;
    }
}