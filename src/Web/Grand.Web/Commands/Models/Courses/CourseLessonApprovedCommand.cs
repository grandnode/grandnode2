using Grand.Domain.Courses;
using Grand.Domain.Customers;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Courses;

public class CourseLessonApprovedCommand : IRequest<bool>
{
    public required Course Course { get; set; }
    public required CourseLesson Lesson { get; set; }
    public required Customer Customer { get; set; }
}