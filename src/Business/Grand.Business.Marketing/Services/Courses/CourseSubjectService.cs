using Grand.Business.Marketing.Interfaces.Courses;
using Grand.Infrastructure.Extensions;
using Grand.Domain.Courses;
using Grand.Domain.Data;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Services.Courses
{
    public class CourseSubjectService : ICourseSubjectService
    {
        private readonly IRepository<CourseSubject> _courseSubjectRepository;
        private readonly IMediator _mediator;

        public CourseSubjectService(IRepository<CourseSubject> courseSubjectRepository, IMediator mediator)
        {
            _courseSubjectRepository = courseSubjectRepository;
            _mediator = mediator;
        }

        public virtual async Task Delete(CourseSubject courseSubject)
        {
            if (courseSubject == null)
                throw new ArgumentNullException(nameof(courseSubject));

            await _courseSubjectRepository.DeleteAsync(courseSubject);

            //event notification
            await _mediator.EntityDeleted(courseSubject);
        }

        public virtual async Task<IList<CourseSubject>> GetByCourseId(string courseId)
        {
            if (string.IsNullOrEmpty(courseId))
                throw new ArgumentNullException(nameof(courseId));

            var query = from c in _courseSubjectRepository.Table
                        where c.CourseId == courseId
                        orderby c.DisplayOrder
                        select c;

            return await Task.FromResult(query.ToList());
        }

        public virtual Task<CourseSubject> GetById(string id)
        {
            return _courseSubjectRepository.GetByIdAsync(id);
        }

        public virtual async Task<CourseSubject> Insert(CourseSubject courseSubject)
        {
            if (courseSubject == null)
                throw new ArgumentNullException(nameof(courseSubject));

            await _courseSubjectRepository.InsertAsync(courseSubject);

            //event notification
            await _mediator.EntityInserted(courseSubject);

            return courseSubject;
        }

        public virtual async Task<CourseSubject> Update(CourseSubject courseSubject)
        {
            if (courseSubject == null)
                throw new ArgumentNullException(nameof(courseSubject));

            await _courseSubjectRepository.UpdateAsync(courseSubject);

            //event notification
            await _mediator.EntityUpdated(courseSubject);

            return courseSubject;
        }
    }
}
