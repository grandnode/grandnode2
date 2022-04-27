using Grand.Domain.Courses;

namespace Grand.Business.Core.Interfaces.Marketing.Courses
{
    public interface ICourseActionService
    {
        Task<CourseAction> GetById(string id);
        Task<CourseAction> GetCourseAction(string customerId, string lessonId);
        Task<bool> CustomerLessonCompleted(string customerId, string lessonId);
        Task<CourseAction> Update(CourseAction courseAction);
        Task<CourseAction> InsertAsync(CourseAction courseAction);
    }
}
