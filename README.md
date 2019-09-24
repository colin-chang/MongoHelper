# MongoHelper

This is a MongoDB operation utility based on `MongoDB.Driver` like the SqlHelper for relation database.It provides the usual CRUD methods.Query for ordered paged and big data are also supported.Index management also included.

Details of how to use it,please check the [unit test project](https://github.com/colin-chang/MongoHelper/tree/master/ColinChang.MongoHelper.Test).

# Serialize & Deserialize

When a Generic method like `Task<IEnumerable<T>> QueryAsync<T>()` is invoked,MongoDriver will try to deserialize the query result to an object of type provided.When the type of the object is complex,it may result in some unexpected exceptions.
* **Numbers**.Use `long` instead of `int`.Use `double` instead of `float`
* **ObjectId**. When the object has an ObjectId type member,a custom [ObjectIdConverter](https://github.com/colin-chang/MongoHelper/blob/master/ColinChang.MongoHelper.JsonConvert/ObjectIdConverter.cs) is required.You can also use [ColinChang.MongoHelper.JsonConvert](https://www.nuget.org/packages/ColinChang.MongoHelper.JsonConvert/) simply to handle this.
* **Subclass**. MongoDriver is not friendly for subclass when deserialize to object.try to use interface rather than BaseClass.
* **Unmapped members**.When the type of your object is not complete mapped with the data from mongo.You should map both of them before invoking mongohelper.

```csharp
var mongo =
    new MongoHelper("mongodb://127.0.0.1","TestDb");
        
BsonClassMap.RegisterClassMap<ImageResult>(map =>
{
    map.AutoMap();
    map.SetIgnoreExtraElements(true);
    map.MapIdField(p => p.Id);
    map.UnmapMember(p=>p.Key);
    
    point.MapProperty(p => ir.UserName).SetElementName("name");
});
```
check more details here http://mongodb.github.io/mongo-csharp-driver/2.7/reference/bson/mapping/



**[Nuget](https://www.nuget.org/packages/ColinChang.MongoHelper/)**
```sh
# Package Manager
Install-Package ColinChang.MongoHelper

# .NET CLI
dotnet add package ColinChang.MongoHelper
```

> Here is the how is works:
https://architecture.colinchang.net/nosql/mongo.html
