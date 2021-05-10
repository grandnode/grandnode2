using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using System;

namespace Grand.Domain.MongoDB
{
    public static class MongoDBMapperConfiguration
    {

        /// <summary>
        /// Register MongoDB mappings
        /// </summary>
        public static void RegisterMongoDBMappings()
        {
            BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
            BsonSerializer.RegisterSerializer(typeof(decimal?), new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));
            BsonSerializer.RegisterSerializer(typeof(DateTime), new BsonUtcDateTimeSerializer());

            //global set an equivalent of [BsonIgnoreExtraElements] for every Domain Model
            var cp = new ConventionPack();
            cp.Add(new IgnoreExtraElementsConvention(true));
            ConventionRegistry.Register("ApplicationConventions", cp, t => true);

        }

    }
}
