using Grand.SharedKernel;
using LiteDB;

namespace Grand.Domain.Data.LiteDb
{
    public class LiteDBStartupBase : IStartupBase
    {
        public int Priority => 0;

        public void Execute()
        {
            BsonMapper.Global.EmptyStringToNull = false;
            BsonMapper.Global.SerializeNullValues = true;
            BsonMapper.Global.EnumAsInteger = true;
        }
    }
}
