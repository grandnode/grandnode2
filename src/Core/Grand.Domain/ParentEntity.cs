using Grand.Domain.Data;
using Grand.SharedKernel.Attributes;

namespace Grand.Domain
{
    public abstract class ParentEntity
    {
        protected ParentEntity()
        {
            _id = UniqueIdentifier.New;
        }

        [DBFieldName("_id")]
        public string Id
        {
            get { return _id; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    _id = UniqueIdentifier.New;
                else
                    _id = value;
            }
        }

        private string _id;

    }
}
