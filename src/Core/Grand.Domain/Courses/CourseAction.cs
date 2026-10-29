using Grand.Domain.Customers;
using Grand.Domain.Audit;

namespace Grand.Domain.Courses;

public class CourseAction : BaseEntity
{
    /// <summary>
    ///     Gets or sets the customer ident
    /// </summary>
    [AuditReference(typeof(Customer))]
    public string CustomerId { get; set; }

    /// <summary>
    ///     Gets or sets the course ident
    /// </summary>
    [AuditReference(typeof(Course))]
    public string CourseId { get; set; }

    /// <summary>
    ///     Gets or sets the lesson ident
    /// </summary>
    [AuditReference(typeof(CourseLesson))]
    public string LessonId { get; set; }

    /// <summary>
    ///     Gets or sets the finished
    /// </summary>
    public bool Finished { get; set; }
}