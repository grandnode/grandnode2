using MongoDB.Bson;

namespace Grand.Domain.Data
{
    public static class UniqueIdentifier
    {
        public static string New => ObjectId.GenerateNewId().ToString();
    }
}
