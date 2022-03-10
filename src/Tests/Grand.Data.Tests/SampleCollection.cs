using Grand.Domain;

namespace Grand.Data.Tests
{
    public class SampleCollection : BaseEntity
    {
        public SampleCollection()
        {
            Phones = new List<string>();
            Category = new List<SampleCategory>();
        }
        public string Name { get; set; }

        public IList<string> Phones { get; set; }

        public IList<SampleCategory> Category { get; set; }

        public class SampleCategory
        {
            public string Name { get; set; }
            public int DisplayOrder { get; set; }
        }

    }
}
