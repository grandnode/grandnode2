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
            get => _id;
            set => _id = string.IsNullOrEmpty(value) ? UniqueIdentifier.New : value;
        }

        private string _id;

    }
}
