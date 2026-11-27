namespace Grand.Domain.Courses;

public class CourseLevel : BaseEntity
{
    /// <summary>
    ///     Gets or sets the name
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the display order
    /// </summary>
    public int DisplayOrder { get; set; }
}