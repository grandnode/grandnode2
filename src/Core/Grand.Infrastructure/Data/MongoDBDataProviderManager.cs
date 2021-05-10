using Grand.Domain.Data;

namespace Grand.Infrastructure.Data
{
    public partial class MongoDBDataProviderManager : BaseDataProviderManager
    {
        public MongoDBDataProviderManager(DataSettings settings) : base(settings)
        {
        }

        public override IDataProvider LoadDataProvider()
        {
            return new MongoDBDataProvider();
        }
    }
}
