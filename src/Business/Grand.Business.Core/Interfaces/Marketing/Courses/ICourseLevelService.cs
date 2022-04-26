using Grand.Domain.Courses;

namespace Grand.Business.Core.Interfaces.Marketing.Courses
{
    public interface ICourseLevelService
    {
        Task<CourseLevel> GetById(string id);
        Task<IList<CourseLevel>> GetAll();
        Task<CourseLevel> Update(CourseLevel courseLevel);
        Task<CourseLevel> Insert(CourseLevel courseLevel);
        Task Delete(CourseLevel courseLevel);
    }
}
