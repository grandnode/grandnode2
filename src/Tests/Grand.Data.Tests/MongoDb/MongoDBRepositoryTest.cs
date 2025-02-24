using Grand.Data.Mongo;
using Grand.Domain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Grand.Data.Tests.MongoDb;

public class MongoDBRepositoryTest<T> : MongoRepository<T> where T : BaseEntity
{
    public MongoDBRepositoryTest() : base(
        DriverTestConfiguration.Client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName),
        new AuditInfoProvider())
    {
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        var cp = new ConventionPack {
            new IgnoreExtraElementsConvention(true)
        };
        ConventionRegistry.Register("ApplicationConventions", cp, t => true);

        var client = DriverTestConfiguration.Client;


        Database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
        Database.DropCollection(typeof(T).Name);
        Collection = Database.GetCollection<T>(typeof(T).Name);
    }
}