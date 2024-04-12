using MongoDB.Bson;

namespace Grand.Domain;

public static class UniqueIdentifier
{
    public static string New => ObjectId.GenerateNewId().ToString();
}