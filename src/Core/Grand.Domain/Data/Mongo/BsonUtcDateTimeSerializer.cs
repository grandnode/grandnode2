﻿using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Grand.Domain.Data.Mongo
{
    public class BsonUtcDateTimeSerializer : DateTimeSerializer
    {
        public override DateTime Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return new DateTime(base.Deserialize(context, args).Ticks, DateTimeKind.Utc);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateTime value)
        {
            var utcValue = new DateTime(value.Ticks, DateTimeKind.Utc);
            base.Serialize(context, args, utcValue);
        }
    }
}
