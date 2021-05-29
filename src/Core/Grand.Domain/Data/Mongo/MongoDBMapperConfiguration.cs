using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;

namespace Grand.Domain.Data.Mongo
{
    public static class MongoDBMapperConfiguration
    {

        /// <summary>
        /// Register MongoDB mappings
        /// </summary>
        public static void RegisterMongoDBMappings()
        {
            //BsonSerializer.RegisterSerializer(new DecimalSerializer(BsonType.Decimal128));
            //BsonSerializer.RegisterSerializer(typeof(double?), new NullableSerializer<double>(new DecimalSerializer(BsonType.Decimal128)));
            BsonSerializer.RegisterSerializer(typeof(DateTime), new BsonUtcDateTimeSerializer());

            BsonSerializer.RegisterSerializer(typeof(Dictionary<int, int>),
                new DictionaryInterfaceImplementerSerializer<Dictionary<int, int>>(DictionaryRepresentation.ArrayOfArrays));

            //global set an equivalent of [BsonIgnoreExtraElements] for every Domain Model
            var cp = new ConventionPack {
                new IgnoreExtraElementsConvention(true)
            };
            ConventionRegistry.Register("ApplicationConventions", cp, t => true);


            BsonClassMap.RegisterClassMap<Media.Download>(cm =>
            {
                cm.AutoMap();
                cm.UnmapMember(m => m.DownloadBinary);
            });
        }

    }
}
