using Grand.Domain.Courses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Interfaces.Courses
{
    public interface ICourseLessonService
    {
        Task<CourseLesson> GetById(string id);
        Task<IList<CourseLesson>> GetByCourseId(string courseId);
        Task<CourseLesson> Update(CourseLesson courseLesson);
        Task<CourseLesson> Insert(CourseLesson courseLesson);
        Task Delete(CourseLesson courseLesson);
    }
}
