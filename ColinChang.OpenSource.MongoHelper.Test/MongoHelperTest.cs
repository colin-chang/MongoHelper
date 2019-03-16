using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using Xunit;
using Xunit.Abstractions;

namespace ColinChang.OpenSource.MongoHelper.Test
{
    public class MongoHelperTest : IClassFixture<MongoFixture>
    {
        private readonly MongoHelper _mongo;
        private readonly string _collection;
        private readonly ITestOutputHelper _testOutputHelper;

        public MongoHelperTest(MongoFixture fixture, ITestOutputHelper testOutputHelper)
        {
            _mongo = fixture.Mongo;
            _collection = fixture.Collection;
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task QueryCountTest()
        {
            var total = await _mongo.QueryCountAsync<Person>(_collection);
            Assert.True(total > 0);

            var adult = await _mongo.QueryCountAsync<Person>(_collection, p => p.Age >= 18);
            Assert.True(adult <= total);
        }

        [Fact]
        public async Task InsertTypeTestAsync()
        {
            var inserting = await _mongo.QueryCountAsync<Person>(_collection);
            await _mongo.InsertAsync<Person>(_collection, new Person("Colin", 8), new Person("Robin", 9));
            var inserted = await _mongo.QueryCountAsync<Person>(_collection);

            Assert.Equal(inserted, inserting + 2);
        }

        [Fact]
        public async Task DeleteTestAsync()
        {
            var deleting = await _mongo.QueryCountAsync<Person>(_collection);
            await _mongo.Delete<Person>(_collection, p => p.Name == "Colin");
            var deleted = await _mongo.QueryCountAsync<Person>(_collection);
            Assert.True(deleted <= deleting);
        }

        [Fact]
        public async Task UpdateTestAsync()
        {
            var updates = new List<UpdateCondition<Person, object>>
            {
                new UpdateCondition<Person, object>(p => p.Name, "Tomas"),
                new UpdateCondition<Person, object>(p => p.Age, 40)
            };
            await _mongo.Update(_collection, updates, p => p.Name == "Tom");


            var tom = await _mongo.QueryCountAsync<Person>(_collection, p => p.Name == "Tom");
            Assert.True(tom <= 0);
        }

        [Fact]
        public async Task QueryTestAsync()
        {
            // where查询
            var persons = await _mongo.QueryAsync<Person>(_collection, p => p.Age > 18);
            Assert.DoesNotContain(persons, p => p.Age <= 18);
        }

        [Fact]
        public async Task QueryPagesTestAsync()
        {
            //分页查询
            var persons = await _mongo.QueryAsync<Person>(_collection, p => p.Age > 18, 2, 3);

            Assert.DoesNotContain(persons, p => p.Age <= 18);
            Assert.True(persons.Count() <= 3);
        }

        [Fact]
        public async Task QuerySortTestAsync()
        {
            //排序
            var scs = new List<SortCondition<Person>>
            {
                new SortCondition<Person>(p => p.Age),
                new SortCondition<Person>(p => p.Name, SortType.Descending)
            };
            var persons = await _mongo.QueryAsync<Person>("persons", sortConditions: scs);

            if (persons.Count() > 2)
                Assert.True(persons.First().Age <= persons.ElementAt(1).Age);

            foreach (var person in persons)
                _testOutputHelper.WriteLine(person.ToString());
        }

        [Fact]
        public async Task QueryBigDataTestAsync()
        {
            //大数据量查询
            using (var cursor = await _mongo.QueryBigDataAsync<Person>(_collection))
                while (cursor.MoveNext())
                    foreach (var person in cursor.Current)
                        _testOutputHelper.WriteLine(person.ToString());
        }
    }

    public class MongoFixture : IDisposable
    {
        private const string ConnStr = "mongodb://colin:123123@127.0.0.1";
        private const string DbName = "test";
        public string Collection { get; } = "persons";

        public MongoHelper Mongo { get; set; }

        public MongoFixture()
        {
            var jsons = new string[]
            {
                "{Name:'Colin',Age:15}",
                "{Name:'Robin',Age:20}",
                "{Name:'Jim',Age:25}",
                "{Name:'Tom',Age:16}",
                "{Name:'Bob',Age:25}",
                "{Name:'Jerry',Age:28}",
                "{Name:'Chris',Age:28}"
            };

            Mongo = new MongoHelper(ConnStr, DbName);
            Mongo.InsertAsync(Collection, jsons).Wait();
        }

        public async void Dispose()
        {
            //await Mongo.DropCollection(Collection);
            await Mongo.DropDatabase(DbName);
        }
    }

    class Person
    {
        public ObjectId Id { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }

        public Person(string name, int age)
        {
            Name = name;
            Age = age;
        }

        public override string ToString() => $"Id:{Id}\tName:{Name}\tAge:{Age}";
    }
}