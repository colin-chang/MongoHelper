using System;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace ColinChang.MongoHelper.JsonConvert
{
    public class ObjectIdConverter : Newtonsoft.Json.JsonConverter
    {
        public static ObjectIdConverter Default { get; }

        static ObjectIdConverter()
        {
            Default = new ObjectIdConverter();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            ObjectId.TryParse(reader.Value as string, out var result);
            return result;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(ObjectId).IsAssignableFrom(objectType);
        }
    }
}