This is a Json convert utility that can serialize and deserialize objects with "ObjectId"(defined in [MongoDB.Driver](https://www.nuget.org/packages/MongoDB.Driver)) type properties.

We also provide an "ObjectIdConverter" that can work with [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/).Actually,the BsonConvert is just a wrapper of JsonConvert defined in Newtonsoft.Json.