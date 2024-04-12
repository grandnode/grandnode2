using Grand.Domain;

namespace Grand.Data.Tests;

public class SampleCollection : BaseEntity
{
    public string Name { get; set; }

    public int Count { get; set; }
    public IList<string> Phones { get; set; } = new List<string>();

    public IList<SampleCategory> Category { get; set; } = new List<SampleCategory>();

    public class SampleCategory
    {
        public string Name { get; set; }
        public int DisplayOrder { get; set; }
    }
}