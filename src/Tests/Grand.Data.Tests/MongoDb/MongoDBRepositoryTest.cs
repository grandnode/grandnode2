using Grand.Data.Mongo;
using Grand.Domain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Grand.Data.Tests.MongoDb;

public class MongoDBRepositoryTest<T> : MongoRepository<T>, IRepository<T> where T : BaseEntity
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


        _database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
        _database.DropCollection(typeof(T).Name);
        _collection = _database.GetCollection<T>(typeof(T).Name);
    }
}