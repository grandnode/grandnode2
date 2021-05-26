using Grand.Business.Marketing.Interfaces.Courses;
using Grand.Infrastructure.Extensions;
using Grand.Domain.Courses;
using Grand.Domain.Data;
using MediatR;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace Grand.Business.Marketing.Services.Courses
{
    public class CourseActionService : ICourseActionService
    {
        private readonly IRepository<CourseAction> _courseActionRepository;
        private readonly IMediator _mediator;

        public CourseActionService(IRepository<CourseAction> courseActionRepository, IMediator mediator)
        {
            _courseActionRepository = courseActionRepository;
            _mediator = mediator;
        }

        public virtual Task<CourseAction> GetById(string id)
        {
            return _courseActionRepository.GetByIdAsync(id);
        }
        public virtual async Task<CourseAction> GetCourseAction(string customerId, string lessonId)
        {
            var query = from a in _courseActionRepository.Table
                        where a.CustomerId == customerId && a.LessonId == lessonId
                        select a;

            return await Task.FromResult(query.FirstOrDefault());
        }

        public virtual async Task<bool> CustomerLessonCompleted(string customerId, string lessonId)
        {
            var query = await Task.FromResult((from a in _courseActionRepository.Table
                               where a.CustomerId == customerId && a.LessonId == lessonId
                               select a).FirstOrDefault());

            return query != null ? query.Finished : false;
        }

        public virtual async Task<CourseAction> InsertAsync(CourseAction courseAction)
        {
            if (courseAction == null)
                throw new ArgumentNullException(nameof(courseAction));

            await _courseActionRepository.InsertAsync(courseAction);

            //event notification
            await _mediator.EntityInserted(courseAction);

            return courseAction;
        }

        public virtual async Task<CourseAction> Update(CourseAction courseAction)
        {
            if (courseAction == null)
                throw new ArgumentNullException(nameof(courseAction));

            await _courseActionRepository.UpdateAsync(courseAction);

            //event notification
            await _mediator.EntityUpdated(courseAction);

            return courseAction;
        }
    }
}
