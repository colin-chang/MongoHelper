namespace ColinChang.MongoHelper.JsonConvert
{
    public static class BsonConvert
    {
        /// <summary>Serializes the specified object with ObjectId type to a JSON string.</summary>
        /// <param name="value">The object to serialize.</param>
        /// <returns>A JSON string representation of the object.</returns>
        public static string SerializeObject(object value) =>
            Newtonsoft.Json.JsonConvert.SerializeObject(value, ObjectIdConverter.Default);

        /// <summary>Deserializes the JSON to the specified .NET type with ObjectId member.</summary>
        /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
        /// <param name="value">The JSON to deserialize.</param>
        /// <returns>The deserialized object from the JSON string.</returns>
        public static T DeserializeObject<T>(string value) =>
            Newtonsoft.Json.JsonConvert.DeserializeObject<T>(value, ObjectIdConverter.Default);
    }
}