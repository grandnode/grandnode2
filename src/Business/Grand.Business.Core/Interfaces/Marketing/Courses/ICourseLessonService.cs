using Grand.Domain.Courses;

namespace Grand.Business.Core.Interfaces.Marketing.Courses
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
