using Grand.Domain.Courses;

namespace Grand.Business.Core.Interfaces.Marketing.Courses
{
    public interface ICourseSubjectService
    {
        Task<CourseSubject> GetById(string id);
        Task<IList<CourseSubject>> GetByCourseId(string courseId);
        Task<CourseSubject> Update(CourseSubject courseSubject);
        Task<CourseSubject> Insert(CourseSubject courseSubject);
        Task Delete(CourseSubject courseSubject);
    }
}
